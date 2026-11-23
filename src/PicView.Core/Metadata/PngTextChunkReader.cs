using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using PicView.Core.DebugTools;

namespace PicView.Core.Metadata;

/// <summary>
/// Minimal PNG chunk walker that extracts the textual chunks (tEXt, zTXt, iTXt).
/// Stable Diffusion tooling stores its generation parameters there, and ImageMagick's
/// ping mode does not reliably surface them, so they are read straight from the file.
/// </summary>
internal static class PngTextChunkReader
{
    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Largest single text chunk that will be decoded. ComfyUI workflows run to a few
    /// hundred KB; anything beyond this is treated as junk rather than allocated.
    /// </summary>
    private const int MaxChunkLength = 16 * 1024 * 1024;

    public static bool IsPng(Stream stream)
    {
        if (!stream.CanSeek || stream.Length < 8)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[8];
        stream.Position = 0;
        stream.ReadExactly(header);
        stream.Position = 0;
        return header.SequenceEqual(Signature);
    }

    /// <summary>
    /// Reads every text chunk into a keyword/value map. Returns null when the file is not
    /// a PNG or carries no text chunks. The first occurrence of a keyword wins.
    /// </summary>
    public static Dictionary<string, string>? Read(Stream stream)
    {
        try
        {
            if (!IsPng(stream))
            {
                return null;
            }

            Dictionary<string, string>? results = null;
            stream.Position = 8;

            Span<byte> chunkHeader = stackalloc byte[8];
            while (stream.Position + 8 <= stream.Length)
            {
                stream.ReadExactly(chunkHeader);
                var length = BinaryPrimitives.ReadUInt32BigEndian(chunkHeader[..4]);
                var type = Encoding.ASCII.GetString(chunkHeader[4..]);

                if (type is "IEND")
                {
                    break;
                }

                // 4 trailing CRC bytes follow every chunk's data.
                if (length > MaxChunkLength || stream.Position + length + 4 > stream.Length)
                {
                    break;
                }

                if (type is "tEXt" or "zTXt" or "iTXt")
                {
                    var data = new byte[length];
                    stream.ReadExactly(data);

                    var entry = type switch
                    {
                        "tEXt" => DecodeText(data),
                        "zTXt" => DecodeCompressedText(data),
                        _ => DecodeInternationalText(data)
                    };

                    if (entry is not null)
                    {
                        results ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        results.TryAdd(entry.Value.Keyword, entry.Value.Text);
                    }

                    stream.Position += 4;
                }
                else
                {
                    stream.Position += length + 4;
                }
            }

            return results;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PngTextChunkReader), nameof(Read), e);
            return null;
        }
    }

    /// <summary>tEXt: keyword, null separator, then Latin-1 text.</summary>
    private static (string Keyword, string Text)? DecodeText(ReadOnlySpan<byte> data)
    {
        var separator = data.IndexOf((byte)0);
        if (separator <= 0)
        {
            return null;
        }

        var keyword = Encoding.Latin1.GetString(data[..separator]);
        var text = Encoding.Latin1.GetString(data[(separator + 1)..]);
        return (keyword, text);
    }

    /// <summary>zTXt: keyword, null separator, compression method, then zlib-deflated Latin-1 text.</summary>
    private static (string Keyword, string Text)? DecodeCompressedText(byte[] data)
    {
        var separator = Array.IndexOf(data, (byte)0);
        if (separator <= 0 || data.Length < separator + 2)
        {
            return null;
        }

        // 0 is the only compression method PNG defines.
        if (data[separator + 1] != 0)
        {
            return null;
        }

        var keyword = Encoding.Latin1.GetString(data, 0, separator);
        var text = Inflate(data.AsSpan(separator + 2), Encoding.Latin1);
        return text is null ? null : (keyword, text);
    }

    /// <summary>
    /// iTXt: keyword, null, compression flag, compression method, language tag, null,
    /// translated keyword, null, then UTF-8 text that is deflated when the flag is set.
    /// </summary>
    private static (string Keyword, string Text)? DecodeInternationalText(byte[] data)
    {
        var separator = Array.IndexOf(data, (byte)0);
        if (separator <= 0 || data.Length < separator + 3)
        {
            return null;
        }

        var keyword = Encoding.Latin1.GetString(data, 0, separator);
        var isCompressed = data[separator + 1] != 0;
        var compressionMethod = data[separator + 2];

        var languageEnd = Array.IndexOf(data, (byte)0, separator + 3);
        if (languageEnd < 0)
        {
            return null;
        }

        var translatedEnd = Array.IndexOf(data, (byte)0, languageEnd + 1);
        if (translatedEnd < 0)
        {
            return null;
        }

        var payload = data.AsSpan(translatedEnd + 1);
        if (!isCompressed)
        {
            return (keyword, Encoding.UTF8.GetString(payload));
        }

        if (compressionMethod != 0)
        {
            return null;
        }

        var text = Inflate(payload, Encoding.UTF8);
        return text is null ? null : (keyword, text);
    }

    private static string? Inflate(ReadOnlySpan<byte> compressed, Encoding encoding)
    {
        try
        {
            using var input = new MemoryStream(compressed.ToArray());
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output, 81920);
            return encoding.GetString(output.ToArray());
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PngTextChunkReader), nameof(Inflate), e);
            return null;
        }
    }
}
