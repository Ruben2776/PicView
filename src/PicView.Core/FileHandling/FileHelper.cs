using System.Text.RegularExpressions;
using PicView.Core.DebugTools;

namespace PicView.Core.FileHandling;

public static partial class FileHelper
{
    /// <summary>
    ///     Renames a file by moving it to a new path. Creates the destination directory if it does not exist.
    ///     Never replaces another file: the rename fails when <paramref name="newPath" /> is taken.
    /// </summary>
    /// <param name="path">The current path of the file.</param>
    /// <param name="newPath">The new path to which the file will be moved.</param>
    /// <returns>
    ///     <c>true</c> if the file is successfully renamed; otherwise, <c>false</c>.
    /// </returns>
    public static bool RenameFile(string path, string newPath)
    {
        try
        {
            if (IsNameTaken(path, newPath))
            {
                return false;
            }

            // 1. Get the directory part of the new file path
            var directoryPath = Path.GetDirectoryName(newPath);

            // 2. Check if a directory path was actually extracted
            if (!string.IsNullOrEmpty(directoryPath))
            {
                // 3. This method creates all directories in the path if they don't
                //    already exist. If they do, it does nothing.
                Directory.CreateDirectory(directoryPath);
            }

            // 4. Move the file, failing instead of overwriting if newPath was taken since the check
            File.Move(path, newPath, false);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FileHelper), nameof(RenameFile), e);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Whether moving <paramref name="path" /> to <paramref name="newPath" /> would replace another file or folder.
    ///     A rename that only changes the letter case is not taken on a case-insensitive file system, where
    ///     <paramref name="newPath" /> already points at the file itself.
    /// </summary>
    public static bool IsNameTaken(string path, string newPath)
    {
        if (!Path.Exists(newPath))
        {
            return false;
        }

        if (!string.Equals(path, newPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Same path apart from letter case: taken only if an entry spelled exactly like newPath exists next to it
        var directory = Path.GetDirectoryName(newPath);
        var newName = Path.GetFileName(newPath);
        if (string.IsNullOrEmpty(directory))
        {
            return false;
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            if (string.Equals(Path.GetFileName(entry), newName, StringComparison.Ordinal))
            {
                return !string.Equals(path, newPath, StringComparison.Ordinal);
            }
        }

        return false;
    }

    [GeneratedRegex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex URLregex();

    /// <summary>
    ///     Returns the URL contained in the given string `value` by matching it against a regex pattern.
    ///     If there's an exception thrown, returns an empty string.
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
            DebugHelper.LogDebug(nameof(FileHelper), nameof(GetURL), e);
            return string.Empty;
        }
    }

    /// <summary>
    ///     Generates a new filename with an incremented number inside parentheses to avoid duplication.
    /// </summary>
    /// <param name="currentFile">The path of the current file.</param>
    /// <returns>
    ///     The path of the new file with an incremented number inside parentheses to avoid duplication.
    /// </returns>
    private static string GenerateUniqueFileName(string currentFile)
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

            if (int.TryParse(numberStr, System.Globalization.CultureInfo.CurrentCulture, out var existingNumber))
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

        return newFile;
    }


    /// <summary>
    /// Determines whether the specified path is writable by attempting to open or create a file with write access.
    /// </summary>
    /// <param name="path">The file path to check for write access.</param>
    /// <returns>
    /// <c>true</c> if the path is writable; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsPathWritable(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Asynchronously duplicates a file by creating a copy with an incremented number inside parentheses
    ///     to avoid name conflicts, and returns the path of the new file. If any exception occurs, returns an empty string.
    /// </summary>
    /// <param name="currentFile">The path of the file to be duplicated.</param>
    /// <returns>
    ///     The path of the new file as the result, or an empty string if any exception occurs.
    /// </returns>
    public static async Task<string> DuplicateAndReturnFileNameAsync(string currentFile)
    {
        try
        {
            var newFile = GenerateUniqueFileName(currentFile);
            var fs = new FileStream(newFile, FileMode.OpenOrCreate, FileAccess.Write);
            await using (fs.ConfigureAwait(false))
            {
                var bytes = await File.ReadAllBytesAsync(currentFile).ConfigureAwait(false);
            await fs.WriteAsync(bytes).ConfigureAwait(false);
            return newFile;
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FileHelper), nameof(DuplicateAndReturnFileNameAsync), e);
            return string.Empty;
        }
    }
    
    /// <summary>
    ///     Ensures that the directory for a file path exists
    /// </summary>
    public static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}