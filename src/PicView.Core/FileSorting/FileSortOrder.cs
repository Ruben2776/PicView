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
}
