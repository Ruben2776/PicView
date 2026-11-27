using System.Runtime.InteropServices;
using PicView.Core.FileSorting;

namespace PicView.Core.WindowsNT.FileSorting;

/// <summary>
/// A shell property, as a PROPERTYKEY names it: the column an Explorer view is sorted or grouped by.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ShellPropertyKey(Guid formatId, uint propertyId) : IEquatable<ShellPropertyKey>
{
    public Guid FormatId = formatId;
    public uint PropertyId = propertyId;

    public readonly bool Equals(ShellPropertyKey other) =>
        FormatId == other.FormatId && PropertyId == other.PropertyId;

    public readonly override bool Equals(object? obj) => obj is ShellPropertyKey other && Equals(other);

    public readonly override int GetHashCode() => HashCode.Combine(FormatId, PropertyId);
}

/// <summary>
/// How one Explorer view shows its folder: the first column it is sorted by, with the direction, and the column it
/// is grouped by, if any, with the direction of the groups.
/// </summary>
public readonly record struct ExplorerView(ShellPropertyKey? SortedBy, bool Ascending, ShellPropertyKey? GroupedBy,
    bool GroupsAscending = true);

/// <summary>
/// The File Order an Explorer view's sort is, where it is one.
/// </summary>
public static class ExplorerSort
{
    // The storage property set, which holds the columns a File Order has a field for
    private static readonly Guid Storage = new("B725F130-47EF-101A-A5F1-02608C9EEBAC");

    /// <summary>
    /// System.ItemDate: the Date column, which picture folders are sorted by unless another is chosen. It shows the
    /// date taken of a file that has one, and else the earlier of its creation and last write time.
    /// </summary>
    public static readonly ShellPropertyKey Date = new(new Guid("F7DB74B4-4287-4103-AFBA-F1B13DCD75CF"), 100);

    /// <summary>System.ItemNameDisplay: the Name column.</summary>
    public static readonly ShellPropertyKey Name = new(Storage, 10);

    /// <summary>System.Size: the Size column.</summary>
    public static readonly ShellPropertyKey Size = new(Storage, 12);

    /// <summary>System.DateModified: the Date modified column.</summary>
    public static readonly ShellPropertyKey DateModified = new(Storage, 14);

    /// <summary>System.DateCreated: the Date created column.</summary>
    public static readonly ShellPropertyKey DateCreated = new(Storage, 15);

    /// <summary>System.DateAccessed: the Date accessed column.</summary>
    public static readonly ShellPropertyKey DateAccessed = new(Storage, 16);

    /// <summary>System.ItemTypeText: the Type column, which sorts by the type's name and not by the extension.</summary>
    public static readonly ShellPropertyKey TypeText = new(Storage, 4);

    /// <summary>
    /// The File Order <paramref name="view"/> shows its folder in, or null when no File Order gives it: a column
    /// with no field (Type, Date taken and the rest), or a view grouped by another column than it is sorted by,
    /// or in the other direction, which orders the groups first.
    /// </summary>
    public static FileOrder? ToFileOrder(in ExplorerView view)
    {
        if (view.SortedBy is not { } sortedBy || FieldOf(sortedBy) is not { } field)
        {
            return null;
        }

        if (view.GroupedBy is { } groupedBy &&
            (!groupedBy.Equals(sortedBy) || view.GroupsAscending != view.Ascending))
        {
            return null;
        }

        return new FileOrder(field, view.Ascending);
    }

    /// <summary>
    /// The one order every view of a folder gives, or null when there is no view, when one of them has no File
    /// Order, or when two of them differ.
    /// </summary>
    public static FileOrder? Agreed(IReadOnlyList<ExplorerView> views)
    {
        FileOrder? agreed = null;
        foreach (var view in views)
        {
            if (ToFileOrder(view) is not { } order || (agreed is { } first && first != order))
            {
                return null;
            }

            agreed = order;
        }

        return agreed;
    }

    private static SortFilesBy? FieldOf(ShellPropertyKey key)
    {
        if (key.Equals(Name))
        {
            return SortFilesBy.Name;
        }

        if (key.Equals(Size))
        {
            return SortFilesBy.FileSize;
        }

        if (key.Equals(DateModified))
        {
            return SortFilesBy.LastWriteTime;
        }

        if (key.Equals(DateCreated))
        {
            return SortFilesBy.CreationTime;
        }

        if (key.Equals(DateAccessed))
        {
            return SortFilesBy.LastAccessTime;
        }

        if (key.Equals(Date))
        {
            return SortFilesBy.Date;
        }

        return null;
    }
}
