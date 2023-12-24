using PicView.Core.Config;
using System.Diagnostics;

namespace PicView.Core.FileHandling;

public static class FileListHelper
{
    /// <summary>
    /// Enumeration of different options to sort files by.
    /// </summary>
    public enum SortFilesBy
    {
        /// <summary>
        /// Sort files by name.
        /// </summary>
        Name = 0,

        /// <summary>
        /// Sort files by file size.
        /// </summary>
        FileSize = 1,

        /// <summary>
        /// Sort files by creation time.
        /// </summary>
        CreationTime = 2,

        /// <summary>
        /// Sort files by extension.
        /// </summary>
        Extension = 3,

        /// <summary>
        /// Sort files by last access time.
        /// </summary>
        LastAccessTime = 4,

        /// <summary>
        /// Sort files by last write time.
        /// </summary>
        LastWriteTime = 5,

        /// <summary>
        /// Sort files randomly.
        /// </summary>
        Random = 6
    }

    public static IEnumerable<string> RetrieveFiles(FileInfo fileInfo)
    {
        if (fileInfo == null)
            return new List<string>();

        // Check if the file is a directory or not
        var isDirectory = fileInfo.Attributes.HasFlag(FileAttributes.Directory);

        // Get the directory path based on whether the file is a directory or not
        var directory = isDirectory ? fileInfo.FullName : fileInfo.DirectoryName;
        if (directory is null)
            return new List<string>();

        string[] enumerable;
        // Check if the subdirectories are to be included in the search
        var recurseSubdirectories =
            SettingsHelper.Settings.Sorting.IncludeSubDirectories && string.IsNullOrWhiteSpace(ArchiveExtraction.TempZipFile);
        try
        {
            // Get the list of files in the directory
            IEnumerable<string> files;
            if (recurseSubdirectories)
            {
                files = Directory.EnumerateFiles(directory, "*.*", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true
                }).AsParallel();
            }
            else
            {
                files = Directory.EnumerateFiles(directory, "*.*", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = false
                });
            }

            enumerable = files as string[] ?? files.ToArray();
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(RetrieveFiles)} {fileInfo.Name} exception:\n{exception.Message}");
#endif
            return new List<string>();
        }

        // Filter out files with invalid extensions
        var extensions = SupportedFiles.FileExtensions;

        return enumerable.Where(IsExtensionValid);

        bool IsExtensionValid(string f)
        {
            return extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase);
        }
    }
}