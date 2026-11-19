using PicView.Core.Config;

namespace PicView.Core.FileSorting;

/// <summary>
/// The order a Main Window's tabs list their files in: a Window Setting.
/// </summary>
public readonly record struct FileOrder(SortFilesBy By, bool Ascending)
{
    /// <summary>
    /// The order a settings record describes. A missing group is Name ascending, and an unknown preference is Name.
    /// </summary>
    public static FileOrder FromRecord(Sorting? sorting)
    {
        if (sorting is null)
        {
            return new FileOrder(SortFilesBy.Name, true);
        }

        var by = sorting.SortPreference is >= (int)SortFilesBy.Name and <= (int)SortFilesBy.Random
            ? (SortFilesBy)sorting.SortPreference
            : SortFilesBy.Name;
        return new FileOrder(by, sorting.Ascending);
    }

    /// <summary>
    /// Writes the field and the direction to the record, and nothing else.
    /// </summary>
    public void StoreTo(Sorting sorting)
    {
        sorting.SortPreference = (int)By;
        sorting.Ascending = Ascending;
    }
}
