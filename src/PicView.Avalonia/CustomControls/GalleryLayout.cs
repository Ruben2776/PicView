using Avalonia;
using Avalonia.Layout;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

/// <summary>
///     What <see cref="GalleryLayout" /> reads from the panel it lays out for.
/// </summary>
public readonly record struct GalleryLayoutOptions(Orientation Orientation, bool IsExpanded, double ItemHeight,
    double ItemWidth, double ItemSpacing, double LineSpacing, double WrapHeightOverride);

/// <summary>
///     Where each Gallery Item sits, and the offsets along the scroll axis that center or hold one.
///     The scroll axis is Y in the vertical Docked Gallery and X everywhere else.
/// </summary>
public sealed class GalleryLayout
{
    private readonly List<Rect> _bounds = [];

    /// <summary>
    ///     The bounds of every item, in item order. <see cref="Calculate" /> and <see cref="Clear" /> change the list
    ///     in place, so read it again after either.
    /// </summary>
    public IReadOnlyList<Rect> Bounds => _bounds;

    /// <summary>
    ///     The size the panel measures to, which is what its scroll viewer scrolls over.
    /// </summary>
    public Size Extent { get; private set; }

    /// <summary>
    ///     Whether the last <see cref="Calculate" /> stacked the items in a single column, so the scroll axis is Y.
    /// </summary>
    public bool ScrollsVertically { get; private set; }

    /// <summary>
    ///     Places every item, replacing the earlier bounds and extent.
    /// </summary>
    /// <param name="items">
    ///     The items in order. Each is <see cref="GalleryLayoutOptions.ItemWidth" /> wide when that is set. Otherwise a
    ///     <see cref="GalleryItemViewModel" /> that has its details takes its width from their aspect ratio, and any
    ///     other item is a square.
    /// </param>
    /// <param name="options">The panel's layout properties.</param>
    /// <param name="availableSize">The size the panel was given.</param>
    public void Calculate(IReadOnlyList<object?> items, GalleryLayoutOptions options, Size availableSize)
    {
        _bounds.Clear();
        ScrollsVertically = !options.IsExpanded && options.Orientation == Orientation.Vertical;

        double currentX = 0;
        double currentY = 0;
        double currentColumnMaxWidth = 0;
        double maxExtentY = 0;
        var currentColumnStartIndex = 0; // Track where the current column starts

        // Use the override if provided, otherwise use the actual available height
        var availableHeight = double.IsNaN(options.WrapHeightOverride) || options.WrapHeightOverride <= 0
            ? availableSize.Height
            : options.WrapHeightOverride;

        foreach (var item in items)
        {
            var itemWidth = options.ItemHeight; // Fallback for square

            // 1. Check if the user forced a specific width (e.g., Square Stretch Mode)
            if (!double.IsNaN(options.ItemWidth))
            {
                itemWidth = options.ItemWidth;
            }
            // 2. Otherwise, dynamically calculate based on aspect ratio.
            // Another thread replaces Details whole, so read it once and take width and height from that one read.
            else if (item is GalleryItemViewModel { Details: { PixelHeight: > 0 } details })
            {
                var aspectRatio = (double)details.PixelWidth / details.PixelHeight;
                itemWidth = options.ItemHeight * aspectRatio;
            }

            if (options.IsExpanded)
            {
                // Vertical Wrapping: Use the locked availableHeight
                if (currentY + options.ItemHeight > availableHeight && currentY > 0)
                {
                    for (var j = currentColumnStartIndex; j < _bounds.Count; j++)
                    {
                        var rect = _bounds[j];
                        _bounds[j] = new Rect(rect.X, rect.Y, currentColumnMaxWidth, rect.Height);
                    }

                    currentY = 0;
                    currentX += currentColumnMaxWidth + options.LineSpacing;
                    currentColumnMaxWidth = 0; // Reset for the new column
                    currentColumnStartIndex = _bounds.Count;
                }

                _bounds.Add(new Rect(currentX, currentY, itemWidth, options.ItemHeight));
                currentY += options.ItemHeight + options.ItemSpacing;
                currentColumnMaxWidth = Math.Max(currentColumnMaxWidth, itemWidth);
                maxExtentY = Math.Max(maxExtentY, currentY - options.ItemSpacing);
            }
            else if (options.Orientation == Orientation.Vertical)
            {
                // Vertical Docked Mode (Single Column) - items fill available width
                _bounds.Add(new Rect(currentX, currentY, availableSize.Width, options.ItemHeight));
                currentY += options.ItemHeight;
            }
            else
            {
                // Horizontal Docked Mode (Single Row) - NO SPACING
                _bounds.Add(new Rect(currentX, currentY, itemWidth, options.ItemHeight));
                currentX += itemWidth;
            }
        }

        if (options.IsExpanded && currentColumnStartIndex < _bounds.Count)
        {
            // Apply max width to the very last column
            for (var j = currentColumnStartIndex; j < _bounds.Count; j++)
            {
                var rect = _bounds[j];
                _bounds[j] = new Rect(rect.X, rect.Y, currentColumnMaxWidth, rect.Height);
            }
        }

        if (!options.IsExpanded)
        {
            // Exact extent without deducting spacing
            Extent = options.Orientation == Orientation.Vertical
                ? new Size(availableSize.Width, currentY)
                : new Size(currentX, options.ItemHeight);
            return;
        }

        var totalWidth = currentX + currentColumnMaxWidth;
        // Guard against Infinity if height is temporarily unconstrained during layout passes
        var totalHeight = double.IsInfinity(availableSize.Height) ? maxExtentY : availableSize.Height;
        Extent = new Size(totalWidth, totalHeight);
    }

    /// <summary>
    ///     Forgets the bounds and the extent, so the layout is as new.
    /// </summary>
    public void Clear()
    {
        _bounds.Clear();
        Extent = default;
        ScrollsVertically = false;
    }

    /// <summary>
    ///     The item's center along the scroll axis.
    /// </summary>
    /// <param name="index">An index into <see cref="Bounds" />.</param>
    public double Middle(int index)
    {
        var bounds = _bounds[index];
        return ScrollsVertically ? bounds.Center.Y : bounds.Center.X;
    }

    /// <summary>
    ///     The scroll offset that puts the item in the middle of a view, clamped to the extent.
    /// </summary>
    /// <param name="index">An index into <see cref="Bounds" />.</param>
    /// <param name="viewportLength">The length of the view along the scroll axis.</param>
    public double CenterOffset(int index, double viewportLength)
        => Clamp(Middle(index) - viewportLength / 2, viewportLength);

    /// <summary>
    ///     The scroll offset that puts the item at a distance from the middle of a view, clamped to the extent.
    /// </summary>
    /// <param name="index">An index into <see cref="Bounds" />.</param>
    /// <param name="distanceFromMiddle">How far the item's middle is past the middle of the view. Negative when before it.</param>
    /// <param name="viewportLength">The length of the view along the scroll axis.</param>
    public double HoldOffset(int index, double distanceFromMiddle, double viewportLength)
        => Clamp(Middle(index) - distanceFromMiddle - viewportLength / 2, viewportLength);

    /// <summary>
    ///     The index of the item whose <see cref="Middle" /> is nearest the position. When two are equally near, or
    ///     several share a middle as a column of the Expanded Gallery does, the lowest index wins.
    /// </summary>
    /// <returns>The index, or -1 when there are no bounds.</returns>
    public int IndexNearest(double position)
    {
        if (_bounds.Count is 0)
        {
            return -1;
        }

        // Middle never decreases with the index, so the nearest middle is next to where the position would go
        var after = FirstIndexAtOrAfter(position);
        if (after == 0)
        {
            return 0;
        }

        if (after == _bounds.Count)
        {
            return FirstIndexAtOrAfter(Middle(_bounds.Count - 1));
        }

        var before = Middle(after - 1);
        return position - before <= Middle(after) - position ? FirstIndexAtOrAfter(before) : after;
    }

    /// <summary>
    ///     The first and last index of the items that intersect the area, as the panel needs to realize them. Items
    ///     between the two can miss the area, as a column's lower items do in the Expanded Gallery.
    /// </summary>
    /// <returns>(-1, -1) when none intersect.</returns>
    public (int First, int Last) IndicesIn(Rect area)
    {
        var first = -1;
        var last = -1;

        for (var i = 0; i < _bounds.Count; i++)
        {
            var bounds = _bounds[i];
            if (area.Intersects(bounds))
            {
                if (first is -1)
                {
                    first = i;
                }

                last = i;
            }
            else if (first is not -1)
            {
                // Early exit: We've completely passed the area
                var pastArea = ScrollsVertically ? bounds.Y > area.Bottom : bounds.X > area.Right;
                if (pastArea)
                {
                    break;
                }
            }
        }

        return (first, last);
    }

    /// <summary>
    ///     The first index whose <see cref="Middle" /> is not before the position, or the count when all are. Middle never
    ///     decreases with the index, so this is a binary search.
    /// </summary>
    private int FirstIndexAtOrAfter(double position)
    {
        var low = 0;
        var high = _bounds.Count;
        while (low < high)
        {
            var mid = low + (high - low) / 2;
            if (Middle(mid) < position)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    /// <summary>
    ///     Keeps an offset within what the view can scroll to: from the start to the end of the extent.
    /// </summary>
    private double Clamp(double offset, double viewportLength)
    {
        var extentLength = ScrollsVertically ? Extent.Height : Extent.Width;
        return Math.Clamp(offset, 0, Math.Max(0, extentLength - viewportLength));
    }
}
