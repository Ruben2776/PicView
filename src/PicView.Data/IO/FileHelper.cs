using System.Globalization;

namespace PicView.Data.IO;

public static class FileHelper
{
    /// <summary>
    /// Returns the human-readable file size for an arbitrary, 64-bit file size
    /// The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
    /// </summary>
    /// <param name="length">FileInfo.Length</param>
    /// <returns></returns>
    /// Credits to http://www.somacon.com/p576.php
    public static string GetSizeReadable(long length)
    {
        var sign = length < 0 ? "-" : string.Empty;
        char prefix;
        double value;

        switch (length)
        {
            // Gigabyte
            case >= 0x40000000:
                prefix = 'G';
                value = length >> 20;
                break;
            // Megabyte
            case >= 0x100000:
                prefix = 'M';
                value = length >> 10;
                break;
            // Kilobyte
            case >= 0x400:
                prefix = 'K';
                value = length;
                break;
            // Byte
            default:
                return length.ToString(sign + "0 B", CultureInfo.CurrentCulture);
        }
        value /= 1024; // Divide by 1024 to get fractional value

        return sign + value.ToString("0.## ", CultureInfo.CurrentCulture) + prefix + 'B';
    }

    public static string Shorten(string fileName, int amount)
    {
        if (fileName.Length < 25) { return fileName; }
            
        fileName = fileName[..amount];
        fileName += "...";
        return fileName;
    }
}