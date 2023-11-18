using PicView.ChangeImage;
using PicView.Properties;
using PicView.SystemIntegration;
using System.Diagnostics;
using System.IO;
using static PicView.FileHandling.ArchiveExtraction;
using SearchOption = System.IO.SearchOption;

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

        #region File List

        /// <summary>
        /// Returns a list of file paths for the specified directory sorted according to the specified sort preference.
        /// </summary>
        /// <param name="fileInfo">The directory to retrieve file paths from.</param>
        /// <returns>A list of file paths sorted according to the specified sort preference.</returns>
        internal static List<string>? FileList(FileInfo fileInfo)
        {
            return Settings.Default.SortPreference switch
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
        }

        /// <summary>
        /// A private function to return a list of file names based on the specified sorting method.
        /// </summary>
        /// <param name="fileInfo">The file information object.</param>
        /// <param name="sortFilesBy">The sorting method to be used for the file names.</param>
        /// <returns>A list of file names.</returns>
        private static List<string>? FileList(FileInfo fileInfo, SortFilesBy sortFilesBy)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (fileInfo == null) return null;

            // Check if the file is a directory or not
            var isDirectory = fileInfo.Attributes.HasFlag(FileAttributes.Directory);

            // Get the directory path based on whether the file is a directory or not
            var directory = isDirectory ? fileInfo.FullName : fileInfo.DirectoryName;
            if (directory is null) return null;

            IEnumerable<string> files;
            string[] enumerable;
            // Check if the subdirectories are to be included in the search
            var recurseSubdirectories =
                Settings.Default.IncludeSubDirectories && string.IsNullOrWhiteSpace(TempZipFile);
            try
            {
                // Get the list of files in the directory
                files = Directory.EnumerateFiles(directory, "*.*", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = recurseSubdirectories,
                });
                enumerable = files as string[] ?? files.ToArray();
                if (recurseSubdirectories)
                {
                    enumerable.AsParallel();
                }
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(FileList)} {fileInfo.Name} exception:\n{exception.Message}");
#endif
                files = Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly);
                enumerable = files as string[] ?? files.ToArray();
            }

            // Filter out files with invalid extensions
            var extensions = SupportedFiles.FileExtensions;

            bool IsExtensionValid(string f)
            {
                return extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase);
            }

            files = enumerable.Where((Func<string, bool>)IsExtensionValid);

            // Sort the file names based on the specified sorting method
            switch (sortFilesBy)
            {
                default:
                case SortFilesBy.Name: // Alphanumeric sort
                    var list = files.ToList();
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
                    var fileInfoList = files.Select(f => new FileInfo(f)).ToList();
                    var sortedBySize = Settings.Default.Ascending
                        ? fileInfoList.OrderBy(f => f.Length)
                        : fileInfoList.OrderByDescending(f => f.Length);
                    return sortedBySize.Select(f => f.FullName).ToList();

                case SortFilesBy.Extension: // Sort by file extension
                    var sortedByExtension = Settings.Default.Ascending
                        ? files.OrderBy(Path.GetExtension)
                        : files.OrderByDescending(Path.GetExtension);
                    return sortedByExtension.ToList();

                case SortFilesBy.CreationTime: // Sort by file creation time
                    var sortedByCreationTime = Settings.Default.Ascending
                        ? files.OrderBy(f => new FileInfo(f).CreationTime)
                        : files.OrderByDescending(f => new FileInfo(f).CreationTime);
                    return sortedByCreationTime.ToList();

                case SortFilesBy.LastAccessTime: // Sort by file last access time
                    var sortedByLastAccessTime = Settings.Default.Ascending
                        ? files.OrderBy(f => new FileInfo(f).LastAccessTime)
                        : files.OrderByDescending(f => new FileInfo(f).LastAccessTime);
                    return sortedByLastAccessTime.ToList();

                case SortFilesBy.LastWriteTime: // Sort by file last write time
                    var sortedByLastWriteTime = Settings.Default.Ascending
                        ? files.OrderBy(f => new FileInfo(f).LastWriteTime)
                        : files.OrderByDescending(f => new FileInfo(f).LastWriteTime);
                    return sortedByLastWriteTime.ToList();

                case SortFilesBy.Random: // Sort files randomly
                    return files.OrderBy(f => Guid.NewGuid()).ToList();
            }
        }

        /// <summary>
        /// Returns the next or previous list of file paths based on the current directory.
        /// </summary>
        /// <param name="next">True to retrieve the next directory, false to retrieve the previous directory.</param>
        /// <returns>
        /// A list of file paths for the next or previous directory.
        /// Returns null if there are no files in the directory or an error occurs.
        /// </returns>
        internal static List<string>? NextFileList(bool next)
        {
            try
            {
                var indexChange = next ? 1 : -1;
                var currentFolder = Path.GetDirectoryName(Navigation.Pics[Navigation.FolderIndex]);
                var parentFolder = Path.GetDirectoryName(currentFolder);
                var directories = Directory.GetDirectories(parentFolder, "*", SearchOption.TopDirectoryOnly);
                var directoryIndex = Array.IndexOf(directories, currentFolder);
                if (Settings.Default.Looping)
                    directoryIndex = (directoryIndex + indexChange + directories.Length) % directories.Length;
                else
                {
                    directoryIndex += indexChange;
                    if (directoryIndex < 0 || directoryIndex >= directories.Length)
                        return null;
                }

                for (var i = directoryIndex; i < directories.Length; i++)
                {
                    var fileInfo = new FileInfo(directories[i]);
                    var fileList = FileList(fileInfo);
                    if (fileList is { Count: > 0 })
                        return fileList;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion File List
    }
}