using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using ZLinq;

namespace PicView.Core.FileSorting;

public static class FileSortOrder
{
    public static SortFilesBy GetSortOrder =>
        Settings.Sorting.SortPreference switch
        {
            0 => SortFilesBy.Name,
            1 => SortFilesBy.FileSize,
            2 => SortFilesBy.CreationTime,
            3 => SortFilesBy.Extension,
            4 => SortFilesBy.LastAccessTime,
            5 => SortFilesBy.LastWriteTime,
            6 => SortFilesBy.Random,
            _ => SortFilesBy.Name,
        };

    public static int InsertSorted(List<FileInfo> list, FileInfo newFile, Func<string, string, int> platformService)
    {
        var ascending = Settings.Sorting.Ascending;

        var comparer = Comparer<FileInfo>.Create(Compare);
        var index = list.BinarySearch(newFile, comparer);

        if (index < 0)
        {
            index = ~index;
        }

        list.Insert(index, newFile);
        return index;

        int Compare(FileInfo x, FileInfo y)
        {
            var result = GetSortOrder switch
            {
                SortFilesBy.Name => platformService(x.Name, y.Name),
                SortFilesBy.FileSize => x.Length.CompareTo(y.Length),
                SortFilesBy.Extension => string.Compare(x.Extension, y.Extension, StringComparison.OrdinalIgnoreCase),
                SortFilesBy.CreationTime => x.CreationTime.CompareTo(y.CreationTime),
                SortFilesBy.LastAccessTime => x.LastAccessTime.CompareTo(y.LastAccessTime),
                SortFilesBy.LastWriteTime => x.LastWriteTime.CompareTo(y.LastWriteTime),
                _ => platformService(x.Name, y.Name)
            };
            return ascending ? result : -result;
        }
    }

    public static FileInfo[] GetSortedArchivesInDirectory(string path, Func<string, string, int> stringComparer)
    {
        try
        {
            var dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                return [];
            }

            var archives = dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .Where(f => f.FullName.IsArchive()).AsValueEnumerable();

            switch (GetSortOrder)
            {
                default:
                case SortFilesBy.Name: // Alphanumeric sort
                    var list = archives.ToArray();
                    if (Settings.Sorting.Ascending)
                    {
                        list.Sort((x, y) => stringComparer(x.Name, y.Name));
                    }
                    else
                    {
                        list.Sort((x, y) => stringComparer(y.Name, x.Name));
                    }
                    return list;

                case SortFilesBy.FileSize: // Sort by file size
                    var sortedBySize = Settings.Sorting.Ascending
                        ? archives.OrderBy(x => x.Length)
                        : archives.OrderByDescending(x => x.Length);
                    return sortedBySize.ToArray();

                case SortFilesBy.Extension: // Sort by file extension
                    var sortedByExtension = Settings.Sorting.Ascending
                        ? archives.OrderBy(x => x.Extension)
                        : archives.OrderByDescending(x => x.Extension);
                    return sortedByExtension.ToArray();

                case SortFilesBy.CreationTime: // Sort by file creation time
                    var sortedByCreationTime = Settings.Sorting.Ascending
                        ? archives.OrderBy(x => x.CreationTime)
                        : archives.OrderByDescending(x => x.CreationTime);
                    return sortedByCreationTime.ToArray();

                case SortFilesBy.LastAccessTime: // Sort by file last access time
                    var sortedByLastAccessTime = Settings.Sorting.Ascending
                        ? archives.OrderBy(x => x.LastAccessTime)
                        : archives.OrderByDescending(x => x.LastAccessTime);
                    return sortedByLastAccessTime.ToArray();

                case SortFilesBy.LastWriteTime: // Sort by file last write time
                    var sortedByLastWriteTime = Settings.Sorting.Ascending
                        ? archives.OrderBy(x => x.LastWriteTime)
                        : archives.OrderByDescending(x => x.LastWriteTime);
                    return sortedByLastWriteTime.ToArray();

                case SortFilesBy.Random: // Sort files randomly
                    return archives.OrderBy(_ => Guid.NewGuid()).ToArray();
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileSortOrder), nameof(GetSortedArchivesInDirectory), ex);
            return [];
        }
    }
}