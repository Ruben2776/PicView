using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ImageMagick;

namespace PicView.Tests.Metadata;

internal static class SdTestFiles
{
    /// <summary>
    /// Writes a PNG containing only a signature, an IHDR, the requested text chunks and an IEND.
    /// It is not a decodable image, which is the point: the reader must walk chunks without one.
    /// </summary>
    internal static string WritePng(params (string Keyword, string Text, bool Compressed)[] textChunks)
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview_sd_{Guid.NewGuid():N}.png");
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);

        stream.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WriteChunk(stream, "IHDR", new byte[13]);

        foreach (var (keyword, text, compressed) in textChunks)
        {
            var payload = new MemoryStream();
            payload.Write(Encoding.Latin1.GetBytes(keyword));
            payload.WriteByte(0);

            if (compressed)
            {
                payload.WriteByte(0); // compression method
                using (var zlib = new ZLibStream(payload, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlib.Write(Encoding.Latin1.GetBytes(text));
                }
            }
            else
            {
                payload.Write(Encoding.Latin1.GetBytes(text));
            }

            WriteChunk(stream, compressed ? "zTXt" : "tEXt", payload.ToArray());
        }

        WriteChunk(stream, "IEND", []);
        return path;
    }

    internal static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        stream.Write(length);
        stream.Write(Encoding.ASCII.GetBytes(type));
        stream.Write(data);

        // The CRC covers type and data. The reader ignores it, the writer checks it
        var crc = 0xFFFFFFFFu;
        foreach (var b in Encoding.ASCII.GetBytes(type).Concat(data))
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        BinaryPrimitives.WriteUInt32BigEndian(length, ~crc);
        stream.Write(length);
    }

    /// <summary>Returns <paramref name="png"/> with a tEXt chunk added in front of its IEND.</summary>
    internal static byte[] InsertTextChunk(byte[] png, string keyword, string text)
    {
        var iend = png.Length - 12;
        using var stream = new MemoryStream();
        stream.Write(png, 0, iend);
        WriteChunk(stream, "tEXt", [.. Encoding.Latin1.GetBytes(keyword), 0, .. Encoding.Latin1.GetBytes(text)]);
        stream.Write(png, iend, 12);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes a small JPEG whose Exif UserComment holds the given raw bytes (including any
    /// character-code prefix).
    /// </summary>
    internal static string WriteJpegWithUserComment(byte[] userCommentBytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview_sd_{Guid.NewGuid():N}.jpg");
        using var image = new MagickImage(MagickColors.Gray, 8, 8);
        image.Format = MagickFormat.Jpeg;
        var profile = new ExifProfile();
        profile.SetValue(ExifTag.UserComment, userCommentBytes);
        image.SetProfile(profile);
        image.Write(path);
        return path;
    }

    /// <summary>
    /// Writes a small JPEG whose Exif is big-endian (MM), as piexif writes it, with a <c>UNICODE\0</c> UserComment in
    /// UTF-16BE. Magick.NET only writes little-endian Exif, so the APP1 segment is built by hand.
    /// </summary>
    internal static string WriteJpegWithBigEndianUnicodeUserComment(string text)
    {
        byte[] comment = [.. "UNICODE\0"u8, .. Encoding.BigEndianUnicode.GetBytes(text)];

        using var tiff = new MemoryStream();
        var word = new byte[4];

        void WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(word, value);
            tiff.Write(word, 0, 2);
        }

        void WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(word, value);
            tiff.Write(word, 0, 4);
        }

        tiff.Write("MM"u8);
        WriteUInt16(42);
        WriteUInt32(8);

        // IFD0, at 8: the Exif IFD pointer only
        const uint exifIfdOffset = 8 + 2 + 12 + 4;
        WriteUInt16(1);
        WriteUInt16(0x8769);
        WriteUInt16(4); // LONG
        WriteUInt32(1);
        WriteUInt32(exifIfdOffset);
        WriteUInt32(0);

        // The Exif IFD: ExifVersion 0230, inline, then the UserComment after the IFD
        const uint commentOffset = exifIfdOffset + 2 + 2 * 12 + 4;
        WriteUInt16(2);
        WriteUInt16(0x9000);
        WriteUInt16(7); // UNDEFINED
        WriteUInt32(4);
        tiff.Write("0230"u8);
        WriteUInt16(0x9286);
        WriteUInt16(7);
        WriteUInt32((uint)comment.Length);
        WriteUInt32(commentOffset);
        WriteUInt32(0);
        tiff.Write(comment);

        byte[] app1 = [.. "Exif\0\0"u8, .. tiff.ToArray()];

        byte[] jpeg;
        using (var image = new MagickImage(MagickColors.Gray, 8, 8))
        {
            image.Format = MagickFormat.Jpeg;
            jpeg = image.ToByteArray();
        }

        var path = Path.Combine(Path.GetTempPath(), $"picview_sd_{Guid.NewGuid():N}.jpg");
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        stream.Write(jpeg, 0, 2); // SOI
        stream.Write([0xFF, 0xE1]);
        BinaryPrimitives.WriteUInt16BigEndian(word, (ushort)(app1.Length + 2));
        stream.Write(word, 0, 2);
        stream.Write(app1);
        stream.Write(jpeg, 2, jpeg.Length - 2);
        return path;
    }

    /// <summary>Same as <see cref="WriteJpegWithUserComment"/>, for WebP.</summary>
    internal static string WriteWebPWithUserComment(byte[] userCommentBytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview_sd_{Guid.NewGuid():N}.webp");
        using var image = new MagickImage(MagickColors.Gray, 8, 8);
        image.Format = MagickFormat.WebP;
        var profile = new ExifProfile();
        profile.SetValue(ExifTag.UserComment, userCommentBytes);
        image.SetProfile(profile);
        image.Write(path);
        return path;
    }

    /// <summary>UserComment bytes: the 8-byte ASCII character code, then the text.</summary>
    internal static byte[] AsciiUserComment(string text) =>
        [.. "ASCII\0\0\0"u8, .. Encoding.ASCII.GetBytes(text)];

    /// <summary>Writes a JPEG carrying the given text in its COM segment.</summary>
    internal static string WriteJpegWithComment(string comment)
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview_sd_{Guid.NewGuid():N}.jpg");
        using var image = new MagickImage(MagickColors.Gray, 8, 8);
        image.Format = MagickFormat.Jpeg;
        image.Comment = comment;
        image.Write(path);
        return path;
    }
}
