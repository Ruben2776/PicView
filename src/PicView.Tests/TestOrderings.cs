using PicView.Core.FileSorting;

namespace PicView.Tests;

/// <summary>
///     The File Orderings tests list files and archive pages with.
/// </summary>
internal static class TestOrderings
{
    /// <summary>By name, ordinal unless a comparison is given, as the tab sorts by default.</summary>
    public static FileOrdering ByName(Func<string, string, int>? compareNames = null, bool ascending = true) =>
        By(SortFilesBy.Name, ascending, compareNames);

    public static FileOrdering By(SortFilesBy by, bool ascending = true, Func<string, string, int>? compareNames = null) =>
        new(new FileOrder(by, ascending), compareNames ?? string.CompareOrdinal);

    /// <summary>
    ///     Makes the order the Default File Order, which a directory no tab lists yet starts with.
    /// </summary>
    public static void SetDefault(SortFilesBy by, bool ascending = true) =>
        new FileOrder(by, ascending).StoreAsDefault(Settings);
}
