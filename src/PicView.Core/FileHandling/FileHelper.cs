using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PicView.Core.FileHandling;

public static partial class FileHelper
{
    /// <summary>
    /// RenameFile method renames a file.
    /// </summary>
    /// <param name="path">The original path of the file.</param>
    /// <param name="newPath">The new path of the file.</param>
    /// <returns>
    /// A boolean indicating whether the file was successfully renamed or not.
    /// </returns>
    public static bool RenameFile(string path, string newPath)
    {
        try
        {
            new FileInfo(newPath).Directory.Create(); // create directory if not exists
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(e.Message);
#endif
        }

        try
        {
            File.Move(path, newPath, true);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(e.Message);
#endif
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the human-readable file size for an arbitrary, 64-bit file size
    /// The default format is "0.## XB", e.g. "4.2 KB" or "1.43 GB"
    /// </summary>
    /// <param name="fileSize">FileInfo.Length</param>
    /// <returns>E.g. "3.34 MB"</returns>
    /// Credits to http://www.somacon.com/p576.php
    public static string GetReadableFileSize(this long fileSize)
    {
        const int kilobyte = 1024;
        double value;
        char prefix;

        switch (fileSize)
        {
            // Gigabyte
            case >= 0x40000000:
                prefix = 'G';
                value = fileSize >> 20;
                break;
            // Megabyte
            case >= 0x100000:
                prefix = 'M';
                value = fileSize >> 10;
                break;
            // Kilobyte
            case >= 0x400:
                prefix = 'K';
                value = fileSize;
                break;

            default:
                return fileSize.ToString("0 B", CultureInfo.CurrentCulture); // Byte
        }

        value /= kilobyte; // Divide by 1024 to get fractional value

        return value.ToString($"0.## {prefix}B", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Shortens the given string `name` to the given `amount` and appends "..." to it.
    /// </summary>
    /// <param name="name">The string to shorten</param>
    /// <param name="amount">The length to shorten the string to</param>
    /// <returns>The shortened string</returns>
    public static string Shorten(this string name, int amount)
    {
        name = name[..amount];
        name += "...";
        return name;
    }

    [GeneratedRegex("\\b(?:https?://|www\\.)\\S+\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex URLregex();

    /// <summary>
    /// Returns the URL contained in the given string `value` by matching it against a regex pattern.
    /// If there's an exception thrown, returns an empty string.
    /// </summary>
    /// <param name="value">The string to find the URL in</param>
    /// <returns>The URL contained in the string, or an empty string if no URL is found or an exception is thrown</returns>
    // ReSharper disable once InconsistentNaming
    public static string GetURL(this string value)
    {
        try
        {
            return URLregex().Match(value).ToString();
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(GetURL)} {value} exception, \n {e.Message}");
#endif
            return string.Empty;
        }
    }

    /// <summary>
    /// Returns the URL path if it exists or an empty string if not.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static string RetrieveFromURL(string value)
    {
        // Check if from URL and download it
        var url = GetURL(value);
        if (string.IsNullOrEmpty(url))
            return string.Empty;
        return File.Exists(ArchiveExtraction.TempFilePath) ? ArchiveExtraction.TempFilePath : string.Empty;
    }
}