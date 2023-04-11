using System.IO;
using PicView.Properties;
using PicView.SystemIntegration;
using static PicView.FileHandling.ArchiveExtraction;

namespace PicView.FileHandling
{
    internal static class FileLists
    {
        /// <summary>
        /// Enumeration of different options to sort files by.
        /// </summary>
        internal enum SortFilesBy
        {
            /// <summary>
            /// Sort files by name.
            /// </summary>
            Name,

            /// <summary>
            /// Sort files by file size.
            /// </summary>
            FileSize,

            /// <summary>
            /// Sort files by creation time.
            /// </summary>
            CreationTime,

            /// <summary>
            /// Sort files by extension.
            /// </summary>
            Extension,

            /// <summary>
            /// Sort files by last access time.
            /// </summary>
            LastAccessTime,

            /// <summary>
            /// Sort files by last write time.
            /// </summary>
            LastWriteTime,

            /// <summary>
            /// Sort files randomly.
            /// </summary>
            Random
        }

        /// <summary>
        /// Returns a list of file paths for the specified directory sorted according to the specified sort preference.
        /// </summary>
        /// <param name="fileInfo">The directory to retrieve file paths from.</param>
        /// <returns>A list of file paths sorted according to the specified sort preference.</returns>
        internal static List<string>? FileList(FileInfo fileInfo) => Settings.Default.SortPreference switch
        {
            0 => FileList(fileInfo, SortFilesBy.Name),
            1 => FileList(fileInfo, SortFilesBy.FileSize),
            2 => FileList(fileInfo, SortFilesBy.CreationTime),
            3 => FileList(fileInfo, SortFilesBy.Extension),
            4 => FileList(fileInfo, SortFilesBy.LastAccessTime),
            5 => FileList(fileInfo, SortFilesBy.LastWriteTime),
            6 => FileList(fileInfo, SortFilesBy.Random),
            _ => FileList(fileInfo, SortFilesBy.Name),
        };

        /// <summary>
        /// A private function to return a list of file names based on the specified sorting method.
        /// </summary>
        /// <param name="fileInfo">The file information object.</param>
        /// <param name="sortFilesBy">The sorting method to be used for the file names.</param>
        /// <returns>A list of file names.</returns>
        private static List<string>? FileList(FileInfo fileInfo, SortFilesBy sortFilesBy)
        {
            switch (fileInfo)
            {
                case null: return null;
                case not null: break;
            }

            // Check if the file is a directory or not
            var isDirectory = FileFunctions.CheckIfDirectoryOrFile(fileInfo.FullName);
            if (!isDirectory.HasValue) { return null; }

            // Get the directory path based on whether the file is a directory or not
            var directory = isDirectory.Value ? fileInfo.FullName : fileInfo.DirectoryName;

            // Check if the subdirectories are to be included in the search
            var searchOption = Settings.Default.IncludeSubDirectories && string.IsNullOrWhiteSpace(TempZipFile)
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            // Get the list of files in the directory
            var items = Directory.EnumerateFiles(directory, "*.*", searchOption);
            var enumerable = items as string[] ?? items.ToArray();
            if (searchOption == SearchOption.AllDirectories)
            {
                enumerable.AsParallel();
            }

            // Filter out files with invalid extensions
            var extensions = SupportedFiles.FileExtensions;
            bool IsExtensionValid(string f) => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase);
            items = enumerable.Where((Func<string, bool>) IsExtensionValid);

            // Sort the file names based on the specified sorting method
            switch (sortFilesBy)
            {
                default:
                case SortFilesBy.Name: // Alphanumeric sort
                    var list = items.ToList();
                    if (Settings.Default.Ascending)
                    {
                        list.Sort(NativeMethods.StrCmpLogicalW);
                    }
                    else
                    {
                        list.Sort((x, y) => NativeMethods.StrCmpLogicalW(y, x));
                    }
                    return list;

                case SortFilesBy.FileSize: // Sort by file size
                    var fileInfoList = items.Select(f => new FileInfo(f)).ToList();
                    var sortedBySize = Settings.Default.Ascending ?
                        fileInfoList.OrderBy(f => f.Length) : fileInfoList.OrderByDescending(f => f.Length);
                    return sortedBySize.Select(f => f.FullName).ToList();

                case SortFilesBy.Extension: // Sort by file extension
                    var sortedByExtension = Settings.Default.Ascending ?
                        items.OrderBy(Path.GetExtension) : items.OrderByDescending(Path.GetExtension);
                    return sortedByExtension.ToList();

                case SortFilesBy.CreationTime: // Sort by file creation time
                    var sortedByCreationTime = Settings.Default.Ascending ?
                        items.OrderBy(f => new FileInfo(f).CreationTime) : items.OrderByDescending(f => new FileInfo(f).CreationTime);
                    return sortedByCreationTime.ToList();

                case SortFilesBy.LastAccessTime: // Sort by file last access time
                    var sortedByLastAccessTime = Settings.Default.Ascending ?
                        items.OrderBy(f => new FileInfo(f).LastAccessTime) : items.OrderByDescending(f => new FileInfo(f).LastAccessTime);
                    return sortedByLastAccessTime.ToList();

                case SortFilesBy.LastWriteTime: // Sort by file last write time
                    var sortedByLastWriteTime = Settings.Default.Ascending ?
                        items.OrderBy(f => new FileInfo(f).LastWriteTime) : items.OrderByDescending(f => new FileInfo(f).LastWriteTime);
                    return sortedByLastWriteTime.ToList();

                case SortFilesBy.Random: // Sort files randomly
                    return items.OrderBy(f => Guid.NewGuid()).ToList();
            }
        }
    }
}