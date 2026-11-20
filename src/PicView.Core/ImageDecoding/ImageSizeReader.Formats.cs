using System.Buffers.Binary;
using Microsoft.Win32.SafeHandles;

namespace PicView.Core.ImageDecoding;

public static partial class ImageSizeReader
{
    /// <summary>
    /// Picks the parser from the signature in <paramref name="header"/>, the first bytes of the file, whatever the
    /// extension says.
    /// </summary>
    /// <returns>Whether a parser knew the format and read its size.</returns>
    private static bool TryReadKnownFormat(SafeFileHandle handle, string path, ReadOnlySpan<byte> header,
        out ImageSize size)
    {
        size = default;
        if (header.Length < 4)
        {
            return false;
        }

        if (header.StartsWith(PngSignature))
        {
            return TryReadPng(header, out size);
        }

        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))
        {
            return TryReadGif(header, out size);
        }

        if (header.StartsWith("BM"u8))
        {
            return TryReadBmp(header, out size);
        }

        if (header.StartsWith("8BPS"u8))
        {
            return TryReadPsd(header, out size);
        }

        if (header.StartsWith("qoif"u8))
        {
            return TryReadQoi(header, out size);
        }

        if (header.StartsWith("DDS "u8))
        {
            return TryReadDds(header, out size);
        }

        if (header.StartsWith(JpegSignature))
        {
            return TryReadJpeg(handle, out size);
        }

        if (header.StartsWith("RIFF"u8) && header.Length >= 12 && header.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return TryReadWebp(header, out size);
        }

        var isLittleEndianTiff = header.StartsWith((ReadOnlySpan<byte>)[0x49, 0x49, 0x2A, 0x00]);
        if (isLittleEndianTiff || header.StartsWith((ReadOnlySpan<byte>)[0x4D, 0x4D, 0x00, 0x2A]))
        {
            // A camera RAW looks like a TIFF whose first image is a preview, so its own decoder has to answer
            return !IsCameraRawExtension(Path.GetExtension(path.AsSpan())) &&
                   TryReadTiff(handle, header, isLittleEndianTiff, out size);
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="extension"/> is one of the camera RAW formats that are built on TIFF.
    /// </summary>
    private static bool IsCameraRawExtension(ReadOnlySpan<char> extension)
    {
        if (extension.Length is < 4 or > 4)
        {
            return false;
        }

        Span<char> lower = stackalloc char[4];
        extension.ToLowerInvariant(lower);
        return lower is ".3fr" or ".arw" or ".cr2" or ".crw" or ".dcr" or ".dng" or ".erf" or ".kdc" or ".mdc"
            or ".mef" or ".mos" or ".mrw" or ".nef" or ".nrw" or ".orf" or ".pef" or ".raw" or ".rw2" or ".srf"
            or ".x3f";
    }

    /// <summary>
    /// Reads the width and height tags of the first image file directory, in the byte order the file names.
    /// </summary>
    private static bool TryReadTiff(SafeFileHandle handle, ReadOnlySpan<byte> header, bool isLittleEndian,
        out ImageSize size)
    {
        size = default;
        if (header.Length < 8)
        {
            return false;
        }

        long offset = ReadUInt32(header[4..], isLittleEndian);
        Span<byte> count = stackalloc byte[2];
        if (!TryReadAt(handle, offset, count))
        {
            return false;
        }

        const int entryLength = 12;
        const int maxEntries = 512;
        const int entriesPerRead = 16;
        const ushort imageWidthTag = 256;
        const ushort imageLengthTag = 257;

        var entryCount = Math.Min((int)ReadUInt16(count, isLittleEndian), maxEntries);
        offset += count.Length;
        Span<byte> entries = stackalloc byte[entryLength * entriesPerRead];
        uint width = 0, height = 0;
        for (var done = 0; done < entryCount; done += entriesPerRead)
        {
            var block = entries[..(Math.Min(entriesPerRead, entryCount - done) * entryLength)];
            if (!TryReadAt(handle, offset + (long)done * entryLength, block))
            {
                return false;
            }

            for (var i = 0; i < block.Length; i += entryLength)
            {
                var entry = block.Slice(i, entryLength);
                var tag = ReadUInt16(entry, isLittleEndian);
                if (tag > imageLengthTag)
                {
                    // The entries are sorted by tag, so neither size can follow
                    return false;
                }

                if (tag is not (imageWidthTag or imageLengthTag))
                {
                    continue;
                }

                // A SHORT or a LONG value, held in the entry itself
                uint value;
                switch (ReadUInt16(entry[2..], isLittleEndian))
                {
                    case 3:
                        value = ReadUInt16(entry[8..], isLittleEndian);
                        break;
                    case 4:
                        value = ReadUInt32(entry[8..], isLittleEndian);
                        break;
                    default:
                        return false;
                }

                if (tag is imageWidthTag)
                {
                    width = value;
                }
                else
                {
                    height = value;
                }

                if (width is not 0 && height is not 0)
                {
                    size = new ImageSize(width, height);
                    return true;
                }
            }
        }

        return false;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> bytes, bool isLittleEndian) => isLittleEndian
        ? BinaryPrimitives.ReadUInt16LittleEndian(bytes)
        : BinaryPrimitives.ReadUInt16BigEndian(bytes);

    private static uint ReadUInt32(ReadOnlySpan<byte> bytes, bool isLittleEndian) => isLittleEndian
        ? BinaryPrimitives.ReadUInt32LittleEndian(bytes)
        : BinaryPrimitives.ReadUInt32BigEndian(bytes);

    /// <summary>
    /// The first chunk after the RIFF header says which of the three kinds of WebP this is, and each keeps its size
    /// in its own way.
    /// </summary>
    private static bool TryReadWebp(ReadOnlySpan<byte> header, out ImageSize size)
    {
        size = default;
        if (header.Length < 30)
        {
            return false;
        }

        var chunk = header.Slice(12, 4);
        if (chunk.SequenceEqual("VP8 "u8))
        {
            // Lossy: after the frame tag and its start code, 14 bits each; the two above them are a scale
            if (!header.Slice(23, 3).SequenceEqual((ReadOnlySpan<byte>)[0x9D, 0x01, 0x2A]))
            {
                return false;
            }

            const int sizeBits = 0x3FFF;
            size = new ImageSize((uint)(BinaryPrimitives.ReadUInt16LittleEndian(header[26..]) & sizeBits),
                (uint)(BinaryPrimitives.ReadUInt16LittleEndian(header[28..]) & sizeBits));
            return true;
        }

        if (chunk.SequenceEqual("VP8L"u8))
        {
            // Lossless: after its signature byte, 14 bits of width then 14 of height, each stored one less
            const byte losslessSignature = 0x2F;
            if (header[20] is not losslessSignature)
            {
                return false;
            }

            const uint sizeBits = 0x3FFF;
            var bits = BinaryPrimitives.ReadUInt32LittleEndian(header[21..]);
            size = new ImageSize((bits & sizeBits) + 1, ((bits >> 14) & sizeBits) + 1);
            return true;
        }

        if (chunk.SequenceEqual("VP8X"u8))
        {
            // Extended, which an animation also is: the canvas, 24 bits each, stored one less
            size = new ImageSize(ReadUInt24LittleEndian(header[24..]) + 1, ReadUInt24LittleEndian(header[27..]) + 1);
            return true;
        }

        return false;
    }

    private static uint ReadUInt24LittleEndian(ReadOnlySpan<byte> bytes) =>
        (uint)(bytes[0] | bytes[1] << 8 | bytes[2] << 16);

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];

    /// <summary>
    /// Hops from segment to segment by their lengths until the frame header, which holds the size. It can sit past
    /// hundreds of kilobytes of metadata, none of which is read.
    /// </summary>
    private static bool TryReadJpeg(SafeFileHandle handle, out ImageSize size)
    {
        size = default;

        // A marker, the segment's length, then what a frame header starts with: precision, height, width
        Span<byte> segment = stackalloc byte[9];
        const int maxSegments = 1024;
        long offset = 2;
        for (var i = 0; i < maxSegments; i++)
        {
            var read = ReadUpTo(handle, offset, segment);
            if (read < 2 || segment[0] is not 0xFF)
            {
                return false;
            }

            var marker = segment[1];
            switch (marker)
            {
                case 0xFF:
                    // A fill byte before the marker
                    offset++;
                    continue;
                case 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7):
                    // Markers that stand alone, without a length
                    offset += 2;
                    continue;
                case 0xD9 or 0xDA:
                    // The end of the image, or the start of its pixel data, without a frame header before it
                    return false;
            }

            if (read < 4)
            {
                return false;
            }

            // The length counts its own two bytes
            var length = BinaryPrimitives.ReadUInt16BigEndian(segment[2..]);
            if (length < 2)
            {
                return false;
            }

            if (IsJpegFrameHeader(marker))
            {
                if (read < segment.Length || length < 7)
                {
                    return false;
                }

                size = new ImageSize(BinaryPrimitives.ReadUInt16BigEndian(segment[7..]),
                    BinaryPrimitives.ReadUInt16BigEndian(segment[5..]));
                return true;
            }

            offset += 2 + length;
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="marker"/> starts a frame: SOF0 to SOF15, whose range also holds three markers that
    /// are something else (the Huffman tables, a reserved one and the arithmetic coding conditions).
    /// </summary>
    private static bool IsJpegFrameHeader(byte marker) =>
        marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC);

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// The IHDR chunk comes first in a PNG and starts with the width and height.
    /// </summary>
    private static bool TryReadPng(ReadOnlySpan<byte> header, out ImageSize size)
    {
        size = default;
        if (header.Length < 24 || !header.Slice(12, 4).SequenceEqual("IHDR"u8))
        {
            return false;
        }

        size = new ImageSize(BinaryPrimitives.ReadUInt32BigEndian(header[16..]),
            BinaryPrimitives.ReadUInt32BigEndian(header[20..]));
        return true;
    }

    /// <summary>
    /// The logical screen: the canvas a GIF's frames are drawn on.
    /// </summary>
    private static bool TryReadGif(ReadOnlySpan<byte> header, out ImageSize size)
    {
        size = default;
        if (header.Length < 10)
        {
            return false;
        }

        size = new ImageSize(BinaryPrimitives.ReadUInt16LittleEndian(header[6..]),
            BinaryPrimitives.ReadUInt16LittleEndian(header[8..]));
        return true;
    }

    /// <summary>
    /// The info header after the 14 bytes of file header. The oldest kind, 12 bytes long, holds 16-bit sizes; every
    /// later kind holds signed 32-bit ones, with a negative height for rows stored top down.
    /// </summary>
    private static bool TryReadBmp(ReadOnlySpan<byte> header, out ImageSize size)
    {
        size = default;
        if (header.Length < 26)
        {
            return false;
        }

        const uint coreHeaderSize = 12;
        if (BinaryPrimitives.ReadUInt32LittleEndian(header[14..]) is coreHeaderSize)
        {
            size = new ImageSize(BinaryPrimitives.ReadUInt16LittleEndian(header[18..]),
                BinaryPrimitives.ReadUInt16LittleEndian(header[20..]));
            return true;
        }

        var width = BinaryPrimitives.ReadInt32LittleEndian(header[18..]);
        var height = BinaryPrimitives.ReadInt32LittleEndian(header[22..]);
        if (width < 0 || height is int.MinValue)
        {
            return false;
        }

        size = new ImageSize((uint)width, (uint)Math.Abs(height));
        return true;
    }

    /// <summary>
    /// A PSD (version 1) or PSB (version 2) header: the height comes before the width.
    /// </summary>
    private static bool TryReadPsd(ReadOnlySpan<byte> header, out ImageSize size)
    {
        size = default;
        if (header.Length < 22 || BinaryPrimitives.ReadUInt16BigEndian(header[4..]) is not (1 or 2))
        {
            return false;
        }

        size = new ImageSize(BinaryPrimitives.ReadUInt32BigEndian(header[18..]),
            BinaryPrimitives.ReadUInt32BigEndian(header[14..]));
        return true;
    }

    private static bool TryReadQoi(ReadOnlySpan<byte> header, out ImageSize size)
    {
        size = default;
        if (header.Length < 12)
        {
            return false;
        }

        size = new ImageSize(BinaryPrimitives.ReadUInt32BigEndian(header[4..]),
            BinaryPrimitives.ReadUInt32BigEndian(header[8..]));
        return true;
    }

    /// <summary>
    /// A DDS header is 124 bytes and says so; the height comes before the width.
    /// </summary>
    private static bool TryReadDds(ReadOnlySpan<byte> header, out ImageSize size)
    {
        size = default;
        const uint headerSize = 124;
        if (header.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(header[4..]) is not headerSize)
        {
            return false;
        }

        size = new ImageSize(BinaryPrimitives.ReadUInt32LittleEndian(header[16..]),
            BinaryPrimitives.ReadUInt32LittleEndian(header[12..]));
        return true;
    }
}
