﻿using System.Diagnostics;
using System.Text.RegularExpressions;

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


    [GeneratedRegex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
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
            var lastParenIndex = fileNameWithoutExtension.LastIndexOf('(');
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
}