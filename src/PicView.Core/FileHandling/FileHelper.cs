﻿using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace PicView.Core.FileHandling;

public static partial class FileHelper
{
    /// <summary>
    /// Renames a file by moving it to a new path. Creates the destination directory if it does not exist.
    /// </summary>
    /// <param name="path">The current path of the file.</param>
    /// <param name="newPath">The new path to which the file will be moved.</param>
    /// <returns>
    /// <c>true</c> if the file is successfully renamed; otherwise, <c>false</c>.
    /// </returns>
    public static bool RenameFile(string path, string newPath)
    {
        try
        {
            new FileInfo(newPath).Directory.Create(); // create directory if not exists

            File.Move(path, newPath, true);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(RenameFile)} {path}, {newPath} exception: \n{e.Message}\n");
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
    /// Generates a new filename with an incremented number inside parentheses to avoid duplication.
    /// </summary>
    /// <param name="currentFile">The path of the current file.</param>
    /// <returns>
    /// The path of the new file with an incremented number inside parentheses to avoid duplication.
    /// </returns>
    public static string DuplicateAndReturnFileName(string currentFile)
    {
        string newFile;
        var dir = Path.GetDirectoryName(currentFile);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentFile);
        var extension = Path.GetExtension(currentFile);

        var i = 1;

        // Check if the original filename already contains parentheses
        if (fileNameWithoutExtension.Contains('(') && fileNameWithoutExtension.EndsWith(')'))
        {
            // Extract the number from the existing parentheses
            var lastParenIndex = fileNameWithoutExtension.LastIndexOf("(", StringComparison.Ordinal);
            var numberStr = fileNameWithoutExtension.Substring(lastParenIndex + 1,
                fileNameWithoutExtension.Length - lastParenIndex - 2);

            if (int.TryParse(numberStr, out var existingNumber))
            {
                i = existingNumber + 1;
                fileNameWithoutExtension = fileNameWithoutExtension[..lastParenIndex].TrimEnd();
            }
        }

        // Generate a new filename with an incremented number inside parentheses
        do
        {
            newFile = Path.Combine(dir, $"{fileNameWithoutExtension}({i++}){extension}");
        } while (File.Exists(newFile));

        // Copy the file to the new location
        File.Copy(currentFile, newFile);
        return newFile;
    }

    public static string? ExtractFileSize(this string input)
    {
        // Define a regular expression pattern to match file size formats like "2GB", "100MB", etc.
        const string pattern = @"(\d+)\s*([KMGTP]B)";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        var match = regex.Match(input);

        return match.Success ? match.Value : null;
    }

    public static long GetFileSizeFromString(string input)
    {
        // Define a regular expression pattern to match file size formats like "2GB", "100MB", etc.
        const string pattern = @"(\d+)\s*([KMGTP]B)";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        var match = regex.Match(input);

        if (!match.Success)
        {
            return -1;
        }

        // Extract the size and unit from the matched groups
        var size = long.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value.ToUpper();

        // Convert the size to bytes based on the unit
        switch (unit)
        {
            case "KB":
                size *= 1024;
                break;

            case "MB":
                size *= 1024 * 1024;
                break;

            case "GB":
                size *= 1024 * 1024 * 1024;
                break;

            case "TB":
                size *= 1024L * 1024 * 1024 * 1024;
                break;

            case "PB":
                size *= 1024L * 1024 * 1024 * 1024 * 1024;
                break;
        }

        return size;

        // If no match is found, return an appropriate value (e.g., -1 indicating an error)
    }

    /// <summary>
    /// Checks if the given directory is empty.
    /// </summary>
    public static bool IsDirectoryEmpty(string path)
    {
        return !Directory.EnumerateFileSystemEntries(path).Any();
    }

    public static bool IsFileInUse(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            // If the file can be opened, it's not in use by another process
            return false;
        }
        catch (IOException)
        {
            // If an IOException occurs, the file is in use by another process
            return true;
        }
    }

    public static async Task<byte[]> GetBytesFromFile(FileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        return await GetBytesFromFile(fileInfo.FullName, useAsync: fileInfo.Length > 1e7, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<byte[]> GetBytesFromFile(string filePath, bool useAsync = false, CancellationToken cancellationToken = default)
    {
        const int bufferSize = 4096;
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, useAsync: useAsync);
        var count = fs.Length;
        if (count >= int.MaxValue)
        {
            var bytes = new List<byte>();
            var buffer = new byte[bufferSize]; // Buffer size for reading chunks from the file
            int bytesRead;
            while ((bytesRead = await fs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                bytes.AddRange(buffer.Take(bytesRead)); // Add the bytes read to the list
                cancellationToken.ThrowIfCancellationRequested();
            }
            return bytes.ToArray();
        }
        else
        {
            var bytes = new byte[count];
            var writeIndex = 0;
            while (writeIndex < count)
            {
                var n = await fs.ReadAsync(bytes.AsMemory(writeIndex, (int)count - writeIndex), cancellationToken).ConfigureAwait(false);
                writeIndex += n;
                cancellationToken.ThrowIfCancellationRequested();
            }
            return bytes;
        }
    }
}