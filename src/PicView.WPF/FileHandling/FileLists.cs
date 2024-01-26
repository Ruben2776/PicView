using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.SystemIntegration;
using System.IO;
using SearchOption = System.IO.SearchOption;

namespace PicView.WPF.FileHandling;

internal static class FileLists
{
    #region File List

    /// <summary>
    /// Returns a list of file paths for the specified directory sorted according to the specified sort preference.
    /// </summary>
    /// <param name="fileInfo">The directory to retrieve file paths from.</param>
    /// <returns>A list of file paths sorted according to the specified sort preference.</returns>
    internal static List<string> FileList(FileInfo fileInfo)
    {
        return FileList(fileInfo, FileListHelper.GetSortOrder());
    }

    /// <summary>
    /// A private function to return a list of file names based on the specified sorting method.
    /// </summary>
    /// <param name="fileInfo">The file information object.</param>
    /// <param name="sortFilesBy">The sorting method to be used for the file names.</param>
    /// <returns>A list of file names.</returns>
    private static List<string> FileList(FileInfo fileInfo, FileListHelper.SortFilesBy sortFilesBy)
    {
        if (fileInfo == null)
            return new List<string>();

        var files = FileListHelper.RetrieveFiles(fileInfo);

        FileUpdateNavigation.Initiate(fileInfo.Attributes.HasFlag(FileAttributes.Directory) ? fileInfo.DirectoryName : Path.GetDirectoryName(fileInfo.FullName));

        // Sort the file names based on the specified sorting method
        switch (sortFilesBy)
        {
            default:
            case FileListHelper.SortFilesBy.Name: // Alphanumeric sort
                var list = files.ToList();
                if (SettingsHelper.Settings.Sorting.Ascending)
                {
                    list.Sort(NativeMethods.StrCmpLogicalW);
                }
                else
                {
                    list.Sort((x, y) => NativeMethods.StrCmpLogicalW(y, x));
                }

                return list;

            case FileListHelper.SortFilesBy.FileSize: // Sort by file size
                var fileInfoList = files.Select(f => new FileInfo(f)).ToList();
                var sortedBySize = SettingsHelper.Settings.Sorting.Ascending
                    ? fileInfoList.OrderBy(f => f.Length)
                    : fileInfoList.OrderByDescending(f => f.Length);
                return sortedBySize.Select(f => f.FullName).ToList();

            case FileListHelper.SortFilesBy.Extension: // Sort by file extension
                var sortedByExtension = SettingsHelper.Settings.Sorting.Ascending
                    ? files.OrderBy(Path.GetExtension)
                    : files.OrderByDescending(Path.GetExtension);
                return sortedByExtension.ToList();

            case FileListHelper.SortFilesBy.CreationTime: // Sort by file creation time
                var sortedByCreationTime = SettingsHelper.Settings.Sorting.Ascending
                    ? files.OrderBy(f => new FileInfo(f).CreationTime)
                    : files.OrderByDescending(f => new FileInfo(f).CreationTime);
                return sortedByCreationTime.ToList();

            case FileListHelper.SortFilesBy.LastAccessTime: // Sort by file last access time
                var sortedByLastAccessTime = SettingsHelper.Settings.Sorting.Ascending
                    ? files.OrderBy(f => new FileInfo(f).LastAccessTime)
                    : files.OrderByDescending(f => new FileInfo(f).LastAccessTime);
                return sortedByLastAccessTime.ToList();

            case FileListHelper.SortFilesBy.LastWriteTime: // Sort by file last write time
                var sortedByLastWriteTime = SettingsHelper.Settings.Sorting.Ascending
                    ? files.OrderBy(f => new FileInfo(f).LastWriteTime)
                    : files.OrderByDescending(f => new FileInfo(f).LastWriteTime);
                return sortedByLastWriteTime.ToList();

            case FileListHelper.SortFilesBy.Random: // Sort files randomly
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
            if (SettingsHelper.Settings.UIProperties.Looping)
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