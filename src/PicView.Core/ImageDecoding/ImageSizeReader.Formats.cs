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

        return false;
    }

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
