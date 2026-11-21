using System.Buffers.Binary;
using Microsoft.Win32.SafeHandles;

namespace PicView.Core.ImageDecoding;

public static partial class ImageSizeReader
{
    /// <summary>
    /// Picks the parser from the signature in <paramref name="header"/>, the first bytes of the file, whatever the
    /// extension says.
    /// </summary>
    /// <param name="upright">Whether to give the size the image is shown at: width and height swapped when its EXIF
    /// orientation is a quarter turn. A format that may hold an orientation the parsers here do not read then answers
    /// false, so a fuller reader decides.</param>
    /// <returns>Whether a parser knew the format and read its size.</returns>
    private static bool TryReadKnownFormat(SafeFileHandle handle, string path, ReadOnlySpan<byte> header,
        bool upright, out ImageSize size)
    {
        size = default;
        if (header.Length < 4)
        {
            return false;
        }

        if (header.StartsWith(PngSignature))
        {
            return TryReadPng(header, out size) && (!upright || HasNoPngExif(handle));
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
            // A PSD can hold EXIF among its image resources
            return !upright && TryReadPsd(header, out size);
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
            return TryReadJpeg(handle, upright, out size);
        }

        if (header.StartsWith("RIFF"u8) && header.Length >= 12 && header.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return TryReadWebp(header, out size) && (!upright || !HasWebpExif(header));
        }

        var isLittleEndianTiff = header.StartsWith((ReadOnlySpan<byte>)[0x49, 0x49, 0x2A, 0x00]);
        if (isLittleEndianTiff || header.StartsWith((ReadOnlySpan<byte>)[0x4D, 0x4D, 0x00, 0x2A]))
        {
            // A camera RAW looks like a TIFF whose first image is a preview, so its own decoder has to answer
            return IsTiffExtension(Path.GetExtension(path.AsSpan())) &&
                   TryReadTiff(handle, header, isLittleEndianTiff, upright, out size);
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="extension"/> says the file is a TIFF itself. Many formats are built on TIFF, the
    /// camera RAW ones above all, and new ones keep coming, so only the TIFF extensions are answered from the header.
    /// </summary>
    private static bool IsTiffExtension(ReadOnlySpan<char> extension) =>
        extension.Equals(".tif", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads the width and height tags of the first image file directory, in the byte order the file names, and its
    /// orientation tag when <paramref name="upright"/>.
    /// </summary>
    private static bool TryReadTiff(SafeFileHandle handle, ReadOnlySpan<byte> header, bool isLittleEndian,
        bool upright, out ImageSize size)
    {
        size = default;
        if (header.Length < 8 ||
            !TryReadIfd(handle, 0, long.MaxValue, ReadUInt32(header[4..], isLittleEndian), isLittleEndian,
                upright ? OrientationTag : ImageLengthTag, out var width, out var height, out var orientation) ||
            width is 0 || height is 0)
        {
            return false;
        }

        size = upright && IsQuarterTurn(orientation) ? new ImageSize(height, width) : new ImageSize(width, height);
        return true;
    }

    private const ushort ImageWidthTag = 256;
    private const ushort ImageLengthTag = 257;
    private const ushort OrientationTag = 274;

    /// <summary>
    /// Whether <paramref name="orientation"/>, an EXIF orientation, stores the image lying on its side: 5 to 8.
    /// </summary>
    private static bool IsQuarterTurn(ushort orientation) => orientation is >= 5 and <= 8;

    /// <summary>
    /// Reads the image width, image length and orientation entries of a TIFF directory, as a TIFF file and the EXIF
    /// data of a JPEG both store them, up to <paramref name="lastTag"/>. Offsets count from
    /// <paramref name="tiffStart"/>, and nothing is read from <paramref name="tiffEnd"/> on.
    /// </summary>
    /// <returns>Whether the entries were read; each one not found is 0. False when the directory is cut short, or
    /// stores one of them in a way not read here.</returns>
    private static bool TryReadIfd(SafeFileHandle handle, long tiffStart, long tiffEnd, uint ifdOffset,
        bool isLittleEndian, ushort lastTag, out uint width, out uint height, out ushort orientation)
    {
        width = 0;
        height = 0;
        orientation = 0;

        const int entryLength = 12;
        const int maxEntries = 512;
        const int entriesPerRead = 16;

        var offset = tiffStart + ifdOffset;
        Span<byte> count = stackalloc byte[2];
        if (offset + count.Length > tiffEnd || !TryReadAt(handle, offset, count))
        {
            return false;
        }

        var entryCount = Math.Min((int)ReadUInt16(count, isLittleEndian), maxEntries);
        offset += count.Length;
        Span<byte> entries = stackalloc byte[entryLength * entriesPerRead];
        for (var done = 0; done < entryCount; done += entriesPerRead)
        {
            var block = entries[..(Math.Min(entriesPerRead, entryCount - done) * entryLength)];
            var blockOffset = offset + (long)done * entryLength;
            if (blockOffset + block.Length > tiffEnd || !TryReadAt(handle, blockOffset, block))
            {
                return false;
            }

            for (var i = 0; i < block.Length; i += entryLength)
            {
                var entry = block.Slice(i, entryLength);
                var tag = ReadUInt16(entry, isLittleEndian);
                if (tag > lastTag)
                {
                    // The entries are sorted by tag, so none of the rest is wanted
                    return true;
                }

                // A SHORT or a LONG value, held in the entry itself
                var type = ReadUInt16(entry[2..], isLittleEndian);
                switch (tag)
                {
                    case ImageWidthTag or ImageLengthTag:
                    {
                        uint value;
                        switch (type)
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

                        if (tag is ImageWidthTag)
                        {
                            width = value;
                        }
                        else
                        {
                            height = value;
                        }

                        break;
                    }
                    case OrientationTag:
                        if (type is not 3)
                        {
                            return false;
                        }

                        orientation = ReadUInt16(entry[8..], isLittleEndian);
                        break;
                }
            }
        }

        return true;
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
    /// hundreds of kilobytes of metadata, of which only the orientation in the EXIF data is read, and only when
    /// <paramref name="upright"/>.
    /// </summary>
    private static bool TryReadJpeg(SafeFileHandle handle, bool upright, out ImageSize size)
    {
        size = default;

        // A marker, the segment's length, then what a frame header starts with: precision, height, width
        Span<byte> segment = stackalloc byte[9];
        // What an EXIF segment holds after its length: its identifier, then the TIFF header
        Span<byte> exif = stackalloc byte[14];
        const int maxSegments = 1024;
        const byte app1 = 0xE1;
        var exifRead = false;
        ushort orientation = 0;
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

                var height = BinaryPrimitives.ReadUInt16BigEndian(segment[5..]);
                var width = BinaryPrimitives.ReadUInt16BigEndian(segment[7..]);
                size = upright && IsQuarterTurn(orientation)
                    ? new ImageSize(height, width)
                    : new ImageSize(width, height);
                return true;
            }

            // The first EXIF segment is the one ImageMagick takes the orientation from
            if (upright && !exifRead && marker is app1 && length >= 2 + exif.Length &&
                TryReadAt(handle, offset + 4, exif) && exif.StartsWith("Exif\0\0"u8))
            {
                exifRead = true;
                var tiff = exif[6..];
                var isLittleEndian = tiff.StartsWith((ReadOnlySpan<byte>)[0x49, 0x49, 0x2A, 0x00]);
                if (!isLittleEndian && !tiff.StartsWith((ReadOnlySpan<byte>)[0x4D, 0x4D, 0x00, 0x2A]))
                {
                    return false;
                }

                if (!TryReadIfd(handle, offset + 10, offset + 2 + length, ReadUInt32(tiff[4..], isLittleEndian),
                        isLittleEndian, OrientationTag, out _, out _, out orientation))
                {
                    return false;
                }
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
    /// Whether the PNG has no EXIF data before its pixel data, where an orientation would be: neither an eXIf chunk
    /// nor a text chunk holding a raw profile, as ImageMagick writes EXIF. Only the chunk headers, and the keywords of
    /// text chunks, are read.
    /// </summary>
    private static bool HasNoPngExif(SafeFileHandle handle)
    {
        const int maxChunks = 1024;
        Span<byte> chunk = stackalloc byte[8];
        Span<byte> keyword = stackalloc byte[RawProfileKeyword.Length];
        long offset = PngSignature.Length;
        for (var i = 0; i < maxChunks; i++)
        {
            if (!TryReadAt(handle, offset, chunk))
            {
                return false;
            }

            var length = BinaryPrimitives.ReadUInt32BigEndian(chunk);
            var type = chunk[4..];
            if (type.SequenceEqual("IDAT"u8) || type.SequenceEqual("IEND"u8))
            {
                return true;
            }

            if (type.SequenceEqual("eXIf"u8))
            {
                return false;
            }

            if ((type.SequenceEqual("tEXt"u8) || type.SequenceEqual("zTXt"u8) || type.SequenceEqual("iTXt"u8)) &&
                length >= keyword.Length && TryReadAt(handle, offset + chunk.Length, keyword) &&
                keyword.SequenceEqual(RawProfileKeyword))
            {
                return false;
            }

            // The length and type before the data, the CRC after it
            offset += 12 + (long)length;
        }

        return false;
    }

    private static ReadOnlySpan<byte> RawProfileKeyword => "Raw profile type "u8;

    /// <summary>
    /// Whether an extended WebP says it holds EXIF data. A simple one cannot.
    /// </summary>
    private static bool HasWebpExif(ReadOnlySpan<byte> header)
    {
        const byte exifFlag = 0x08;
        return header.Slice(12, 4).SequenceEqual("VP8X"u8) && (header[20] & exifFlag) is not 0;
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
        var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(header[14..]);

        // The sizes the kinds of info header have. Two letters are a weak signature, so anything else is not a BMP
        if (headerSize is not (coreHeaderSize or 16 or 40 or 52 or 56 or 64 or 108 or 124))
        {
            return false;
        }

        if (headerSize is coreHeaderSize)
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
