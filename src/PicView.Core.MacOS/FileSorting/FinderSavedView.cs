namespace PicView.Core.MacOS.FileSorting;

/// <summary>
/// The view settings Finder saved for one folder, as its parent's <c>.DS_Store</c> keeps them. Each is null when the
/// file holds no such setting for the folder.
/// </summary>
/// <param name="GroupBy">What the folder's items are grouped by: the <c>GRP0</c> record, as Finder names it.</param>
/// <param name="IconArrangeBy">How the folder's icon view is arranged: the <c>arrangeBy</c> of its <c>icvp</c> record.</param>
/// <param name="ListSortColumn">
/// The column the folder's list view is sorted by: the <c>sortColumn</c> of its <c>lsvC</c> record, else of its
/// older <c>lsvp</c> record.
/// </param>
/// <param name="ListSortAscending">Whether that column sorts ascending, as the same record says.</param>
public readonly record struct FinderSavedView(
    string? GroupBy,
    string? IconArrangeBy,
    string? ListSortColumn,
    bool? ListSortAscending);
