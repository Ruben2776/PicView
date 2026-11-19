namespace PicView.Core.FileSorting;

/// <summary>
/// What an archive entry offers for ordering before it is extracted.
/// </summary>
public readonly record struct ArchiveEntryOrderKey(string Name, string Key, long Size, DateTime? Modified);

/// <summary>
/// A <see cref="FileOrder"/> with the name comparison to apply it with: the one comparison for files and archive entries.
/// </summary>
public sealed class FileOrdering(FileOrder order, Func<string, string, int> compareNames)
{
    // What the file system reports for a file that does not exist.
    private static readonly DateTime MissingFileTime = DateTime.FromFileTimeUtc(0);

    public FileOrder Order { get; } = order;

    /// <summary>
    /// The name comparison alone, with the direction applied.
    /// </summary>
    public int CompareNames(string a, string b) => Direction(compareNames(a, b));

    /// <summary>
    /// Never called for Random.
    /// </summary>
    public int Compare(FileInfo a, FileInfo b)
    {
        var result = CompareField(a, b);
        if (result == 0)
        {
            result = compareNames(a.Name, b.Name);
        }

        if (result == 0)
        {
            result = string.CompareOrdinal(a.FullName, b.FullName);
        }

        return Direction(result);
    }

    public int Compare(in ArchiveEntryOrderKey a, in ArchiveEntryOrderKey b)
    {
        var result = CompareField(a, b);
        if (result == 0)
        {
            result = compareNames(a.Name, b.Name);
        }

        if (result == 0)
        {
            result = string.CompareOrdinal(a.Key, b.Key);
        }

        return Direction(result);
    }

    /// <summary>
    /// The files in this order. Random shuffles them.
    /// </summary>
    public List<FileInfo> Sort(IEnumerable<FileInfo> files)
    {
        var list = new List<FileInfo>(files);
        if (Order.By == SortFilesBy.Random)
        {
            Random.Shared.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list));
            return list;
        }

        // The comparison ends in the full path, so no two distinct files tie and an unstable sort cannot reorder any.
        list.Sort(Compare);
        return list;
    }

    /// <summary>
    /// Where <paramref name="file"/> goes in <paramref name="sorted"/>. Random appends.
    /// </summary>
    public int InsertPosition(IReadOnlyList<FileInfo> sorted, FileInfo file)
    {
        if (Order.By == SortFilesBy.Random)
        {
            return sorted.Count;
        }

        var low = 0;
        var high = sorted.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (Compare(sorted[middle], file) <= 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    // The sign alone is flipped, as negating int.MinValue gives int.MinValue
    private int Direction(int result) => Order.Ascending ? result : -Math.Sign(result);

    // The same extension for a file and an entry of the same name, compared without a string per comparison
    private static int CompareExtensions(string a, string b) =>
        Path.GetExtension(a.AsSpan()).CompareTo(Path.GetExtension(b.AsSpan()), StringComparison.OrdinalIgnoreCase);

    private int CompareField(FileInfo a, FileInfo b) => Order.By switch
    {
        SortFilesBy.FileSize => SizeOf(a).CompareTo(SizeOf(b)),
        SortFilesBy.CreationTime => TimeOf(a, SortFilesBy.CreationTime).CompareTo(TimeOf(b, SortFilesBy.CreationTime)),
        SortFilesBy.LastAccessTime => TimeOf(a, SortFilesBy.LastAccessTime).CompareTo(TimeOf(b, SortFilesBy.LastAccessTime)),
        SortFilesBy.LastWriteTime => TimeOf(a, SortFilesBy.LastWriteTime).CompareTo(TimeOf(b, SortFilesBy.LastWriteTime)),
        SortFilesBy.Extension => CompareExtensions(a.Name, b.Name),
        _ => compareNames(a.Name, b.Name)
    };

    private int CompareField(in ArchiveEntryOrderKey a, in ArchiveEntryOrderKey b) => Order.By switch
    {
        SortFilesBy.FileSize => a.Size.CompareTo(b.Size),
        SortFilesBy.CreationTime or SortFilesBy.LastAccessTime or SortFilesBy.LastWriteTime =>
            (a.Modified ?? DateTime.MinValue).CompareTo(b.Modified ?? DateTime.MinValue),
        SortFilesBy.Extension => CompareExtensions(a.Name, b.Name),
        _ => compareNames(a.Name, b.Name)
    };

    private static long SizeOf(FileInfo file)
    {
        try
        {
            return file.Length;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
    }

    private static DateTime TimeOf(FileInfo file, SortFilesBy field)
    {
        try
        {
            var time = field switch
            {
                SortFilesBy.CreationTime => file.CreationTimeUtc,
                SortFilesBy.LastAccessTime => file.LastAccessTimeUtc,
                _ => file.LastWriteTimeUtc
            };
            return time == MissingFileTime ? DateTime.MinValue : time;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return DateTime.MinValue;
        }
    }
}
