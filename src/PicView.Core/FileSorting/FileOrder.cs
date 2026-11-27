using PicView.Core.Config;

namespace PicView.Core.FileSorting;

/// <summary>
/// The order the tabs that list a directory list its files in.
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
    /// The order the settings record describes, for code that needs the Default File Order without reading the record's
    /// Sorting group itself. A missing record is Name ascending.
    /// </summary>
    public static FileOrder FromAppSettings(AppSettings? record) => FromRecord(record?.Sorting);

    /// <summary>
    /// Makes this the Default File Order of <paramref name="record"/>. A missing record keeps nothing.
    /// </summary>
    public void StoreAsDefault(AppSettings? record)
    {
        if (record is not null)
        {
            StoreTo(record.Sorting ??= new Sorting());
        }
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
