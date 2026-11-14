using System.IO.Compression;
using System.Text;

namespace PicView.Core.Svg;

public static class SvgFileReader
{
    /// <summary>
    ///     A file, or its decompressed content, larger than this many bytes is not read: reading throws an
    ///     <see cref="InvalidDataException" />.
    /// </summary>
    internal const int MaxBytes = 256 * 1024 * 1024;

    /// <summary>True when <paramref name="path" /> ends in .svg or .svgz, in any letter case.</summary>
    public static bool HasSvgExtension(string path)
    {
        var extension = Path.GetExtension(path.AsSpan());
        return extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".svgz", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Reads an SVG file as text. Decides by content, not extension: gzip data is decompressed, then a UTF-8,
    ///     UTF-16 LE or UTF-16 BE byte order mark selects the encoding, and no mark means UTF-8. Throws an
    ///     <see cref="InvalidDataException" /> when the file or its decompressed content is larger than
    ///     <see cref="MaxBytes" />.
    /// </summary>
    public static Task<string> ReadAsync(string path, CancellationToken ct = default)
    {
        return ReadAsync(path, MaxBytes, ct);
    }

    internal static async Task<string> ReadAsync(string path, int maxBytes, CancellationToken ct)
    {
        byte[] bytes;
        var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        await using (file.ConfigureAwait(false))
        {
            if (file.Length > maxBytes)
            {
                throw new InvalidDataException($"The file is larger than {maxBytes} bytes.");
            }

            bytes = new byte[file.Length];
            await file.ReadExactlyAsync(bytes, ct).ConfigureAwait(false);
        }

        if (bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B)
        {
            bytes = await GunzipAsync(bytes, maxBytes, ct).ConfigureAwait(false);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>Decompresses gzip data, stopping as soon as the output passes <paramref name="maxBytes" />.</summary>
    private static async Task<byte[]> GunzipAsync(byte[] compressed, int maxBytes, CancellationToken ct)
    {
        using var source = new MemoryStream(compressed);
        var gzip = new GZipStream(source, CompressionMode.Decompress);
        await using var _ = gzip.ConfigureAwait(false);
        using var unzipped = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await gzip.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            if (unzipped.Length + read > maxBytes)
            {
                throw new InvalidDataException($"The decompressed file is larger than {maxBytes} bytes.");
            }

            unzipped.Write(buffer, 0, read);
        }

        return unzipped.ToArray();
    }
}
