using System.IO.Compression;
using System.Text;

namespace PicView.Core.Svg;

public static class SvgFileReader
{
    /// <summary>
    ///     Reads an SVG file as text. Decides by content, not extension: gzip data is decompressed, then a UTF-8,
    ///     UTF-16 LE or UTF-16 BE byte order mark selects the encoding, and no mark means UTF-8.
    /// </summary>
    public static async Task<string> ReadAsync(string path, CancellationToken ct = default)
    {
        var bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);

        if (bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B)
        {
            using var source = new MemoryStream(bytes);
            var gzip = new GZipStream(source, CompressionMode.Decompress);
            await using var _ = gzip.ConfigureAwait(false);
            using var unzipped = new MemoryStream();
            await gzip.CopyToAsync(unzipped, ct).ConfigureAwait(false);
            bytes = unzipped.ToArray();
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
}
