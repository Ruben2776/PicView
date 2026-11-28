using PicView.Core.FileSorting;

namespace PicView.Core.MacOS.FileSorting;

/// <summary>
/// How one Finder window shows its folder, as Finder's scripting tells it: the folder, the view, the column a list
/// view is sorted by with its direction, and how an icon view is arranged. Each but the folder is a four-character
/// code, so none depends on Finder's language. A Finder tab is a window of its own here.
/// </summary>
/// <param name="Folder">The path of the folder shown; null for a window that shows none, as a search does.</param>
/// <param name="View">The view the window shows its folder in.</param>
/// <param name="SortedBy">The column a list view is sorted by.</param>
/// <param name="Direction">The direction a list view is sorted in.</param>
/// <param name="Arrangement">How an icon view arranges its items; 0 when not read.</param>
public readonly record struct FinderWindow(string? Folder, uint View, uint SortedBy, uint Direction, uint Arrangement = 0);

/// <summary>
/// The File Order a Finder window's sort is, where it is one (ADR-0050, On macOS).
/// </summary>
public static class FinderSort
{
    /// <summary>The list view: its sort is the column it is sorted by, in the direction it is sorted in.</summary>
    public static readonly uint ListView = Code("lsvw");

    /// <summary>The icon view: its sort is the way it is arranged.</summary>
    public static readonly uint IconView = Code("icnv");

    /// <summary>An icon view arranged by Name.</summary>
    public static readonly uint ArrangedByName = Code("nama");

    /// <summary>An icon view arranged by Date Modified.</summary>
    public static readonly uint ArrangedByModificationDate = Code("mdta");

    /// <summary>An icon view arranged by Date Created.</summary>
    public static readonly uint ArrangedByCreationDate = Code("cdta");

    /// <summary>An icon view arranged by Size.</summary>
    public static readonly uint ArrangedBySize = Code("siza");

    /// <summary>An icon view arranged by Kind.</summary>
    public static readonly uint ArrangedByKind = Code("kina");

    /// <summary>An icon view not arranged: its items sit where the user put them.</summary>
    public static readonly uint NotArranged = Code("narr");

    /// <summary>The Name column. Finder also answers it for a column its scripting has no name for, as Date Added.</summary>
    public static readonly uint Name = Code("elsn");

    /// <summary>The Size column.</summary>
    public static readonly uint Size = Code("elss");

    /// <summary>The Date Modified column.</summary>
    public static readonly uint DateModified = Code("elsm");

    /// <summary>The Date Created column.</summary>
    public static readonly uint DateCreated = Code("elsc");

    /// <summary>The Kind column, which sorts by the kind's name; the closest File Order is the extension.</summary>
    public static readonly uint Kind = Code("elsk");

    /// <summary>The direction Finder calls normal: ascending.</summary>
    public static readonly uint Normal = Code("snrm");

    /// <summary>The direction Finder calls reversed: descending.</summary>
    public static readonly uint Reversed = Code("srvs");

    /// <summary>
    /// The number an Apple Event names <paramref name="code"/> by: its four characters, the first highest.
    /// </summary>
    public static uint Code(string code)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(code.Length, 4);
        return ((uint)code[0] << 24) | ((uint)code[1] << 16) | ((uint)code[2] << 8) | code[3];
    }

    /// <summary>
    /// The File Order <paramref name="window"/> shows its folder in, or null when no File Order gives it: a view
    /// other than the list view and the icon view, a column or an arrangement with no field (Tags, not arranged and
    /// the rest), or a direction not known. An icon view has no direction of its own: Finder sorts Name and Kind
    /// ascending, and Size and the dates descending.
    /// </summary>
    public static FileOrder? ToFileOrder(in FinderWindow window)
    {
        if (window.View == IconView)
        {
            return ArrangementOrder(window.Arrangement);
        }

        if (window.View != ListView || FieldOf(window.SortedBy) is not { } field)
        {
            return null;
        }

        if (window.Direction == Normal)
        {
            return new FileOrder(field, true);
        }

        return window.Direction == Reversed ? new FileOrder(field, false) : null;
    }

    /// <summary>
    /// The order <paramref name="directory"/> is followed in: that of the first of <paramref name="windows"/>, which
    /// come front to back, that shows it, as that is the one the user sees and last used. Null when none shows it, or
    /// when the one in front is in no File Order, as a window behind it is not what the user sees.
    /// </summary>
    public static FileOrder? Followed(ReadOnlySpan<FinderWindow> windows, string directory)
    {
        foreach (ref readonly var window in windows)
        {
            if (Shows(window.Folder, directory))
            {
                return ToFileOrder(window);
            }
        }

        return null;
    }

    /// <summary>
    /// Whether <paramref name="folder"/>, as a window shows it, is <paramref name="directory"/>: the same path,
    /// without case, whatever separator ends either, and whichever way either writes an accented letter.
    /// </summary>
    public static bool Shows(string? folder, string directory) =>
        DirectoryOrders.KeyOf(folder) is { } shown && DirectoryOrders.KeyOf(directory) is { } asked &&
        string.Equals(Composed(shown), Composed(asked), StringComparison.OrdinalIgnoreCase);

    // A disk may keep a name with its accents apart from their letters, and Finder may not tell it the same way
    private static string Composed(string path) => path.IsNormalized() ? path : path.Normalize();

    private static FileOrder? ArrangementOrder(uint arrangement)
    {
        if (arrangement == ArrangedByName)
        {
            return new FileOrder(SortFilesBy.Name, true);
        }

        if (arrangement == ArrangedByKind)
        {
            return new FileOrder(SortFilesBy.Extension, true);
        }

        if (arrangement == ArrangedBySize)
        {
            return new FileOrder(SortFilesBy.FileSize, false);
        }

        if (arrangement == ArrangedByModificationDate)
        {
            return new FileOrder(SortFilesBy.LastWriteTime, false);
        }

        return arrangement == ArrangedByCreationDate ? new FileOrder(SortFilesBy.CreationTime, false) : null;
    }

    private static SortFilesBy? FieldOf(uint column)
    {
        if (column == Name)
        {
            return SortFilesBy.Name;
        }

        if (column == Size)
        {
            return SortFilesBy.FileSize;
        }

        if (column == DateModified)
        {
            return SortFilesBy.LastWriteTime;
        }

        if (column == DateCreated)
        {
            return SortFilesBy.CreationTime;
        }

        return column == Kind ? SortFilesBy.Extension : null;
    }
}
