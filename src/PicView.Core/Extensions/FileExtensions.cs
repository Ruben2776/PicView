using System.Globalization;

namespace PicView.Core.Extensions;
public static class FileExtensions
{
    /// <summary>
    /// Returns the human-readable file size for an arbitrary, 64-bit file size
    /// The default format is "0.## XB", e.g. "4.2 KB" or "1.43 GB"
    /// </summary>
    /// <param name="fileSize">FileInfo.Length</param>
    public static string GetReadableFileSize(this long fileSize)
    {
        double value;
        char prefix;

        const long gigabyte = 0x40000000;
        const long megabyte = 0x100000;
        const long kilobyte = 0x400;

        switch (fileSize)
        {
            // Gigabyte
            case >= gigabyte:
                prefix = 'G';
                value = (double)fileSize / gigabyte;
                break;
            // Megabyte
            case >= megabyte:
                prefix = 'M';
                value = (double)fileSize / megabyte;
                break;
            // Kilobyte
            case >= kilobyte:
                prefix = 'K';
                value = (double)fileSize / kilobyte;
                break;
            // Byte
            default:
                return fileSize.ToString("0 B", CultureInfo.CurrentCulture);
        }

        return value.ToString($"0.## {prefix}B", CultureInfo.CurrentCulture);
    }
    
    public static IEnumerable<T> OrderBySequence<T, TId>(this IEnumerable<T> source,
        IEnumerable<TId> order, Func<T, TId> idSelector) where TId : notnull
    {
        var lookup = source?.ToDictionary(idSelector, t => t);
        foreach (var id in order)
        {
            yield return lookup[id];
        }
    }
}
