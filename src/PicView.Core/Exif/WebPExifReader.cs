using System.Buffers.Binary;
using ImageMagick;
using PicView.Core.DebugTools;

namespace PicView.Core.Exif;

/// <summary>
/// Reads the Exif profile of a WebP from its EXIF chunk. Magick.NET's Ping does not give a WebP's profile, and a full
/// read decodes the pixels, so the chunks are walked instead.
/// </summary>
internal static class WebPExifReader
{
    /// <returns>The profile, or null when the file is not a WebP or has no EXIF chunk.</returns>
    internal static ExifProfile? ReadProfile(FileInfo fileInfo)
    {
        try
        {
            using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096);
            Span<byte> header = stackalloc byte[12];
            if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length ||
                !header[..4].SequenceEqual("RIFF"u8) || !header[8..].SequenceEqual("WEBP"u8))
            {
                return null;
            }

            Span<byte> chunk = stackalloc byte[8];
            while (stream.ReadAtLeast(chunk, chunk.Length, throwOnEndOfStream: false) == chunk.Length)
            {
                var size = BinaryPrimitives.ReadUInt32LittleEndian(chunk[4..]);
                if (size > stream.Length - stream.Position)
                {
                    return null;
                }

                if (!chunk[..4].SequenceEqual("EXIF"u8))
                {
                    // A chunk's data is padded to an even length
                    stream.Position += size + (size & 1);
                    continue;
                }

                var data = new byte[size];
                stream.ReadExactly(data);

                if (size == 0)
                {
                    return null;
                }

                // Magick.NET parses the data of a JPEG APP1 segment, which starts with "Exif\0\0". WebP writers
                // mostly leave it out and start the chunk with the TIFF header
                return new ExifProfile(data.AsSpan().StartsWith("Exif\0\0"u8) ? data : [.. "Exif\0\0"u8, .. data]);
            }

            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(WebPExifReader), nameof(ReadProfile), e);
            return null;
        }
    }
}
