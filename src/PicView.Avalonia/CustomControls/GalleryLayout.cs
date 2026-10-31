using Avalonia;
using Avalonia.Layout;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

/// <summary>
///     What <see cref="GalleryLayout" /> reads from the panel it lays out for.
/// </summary>
/// <param name="LayoutScale">
///     Device pixels per DIP, as layout rounding uses it: <see cref="LayoutHelper.GetLayoutScale" /> of the panel.
/// </param>
public readonly record struct GalleryLayoutOptions(Orientation Orientation, bool IsExpanded, double ItemHeight,
    double ItemWidth, double ItemSpacing, double LineSpacing, double WrapHeightOverride, double LayoutScale = 1);

/// <summary>
///     Where each Gallery Item sits, and the offsets along the scroll axis that center or hold one.
///     The scroll axis is Y in the vertical Docked Gallery and X everywhere else.
///     <para>
///         The bounds are in the panel's coordinates. The offsets are the scroll viewer's, which scrolls over the
///         panel's margin too, as the Expanded Gallery has one: the items start that margin into what it scrolls.
///     </para>
///     <para>
///         Widths and spacings are whole device pixels, so along a row and across the Expanded Gallery's columns every
///         item starts on a device pixel, where layout rounding puts its container, and a width read later moves the
///         items after it by whole device pixels. The offsets that center an item, and the moves of a view, are whole
///         device pixels too. A view that moves with the items then keeps each one it shows on its pixel: layout rounds
///         the strip's origin and each container on their own, and anything off the pixels would round them a pixel
///         apart.
///     </para>
/// </summary>
public sealed class GalleryLayout
{
    /// <summary>
    ///     How far from a device pixel, in device pixels, an offset can be and still count as on it. Sums of widths
    ///     in DIPs carry rounding errors far below this.
    /// </summary>
    private const double OnPixelTolerance = 1e-6;

    private readonly List<Rect> _bounds = [];

    /// <summary>
    ///     The last <see cref="Calculate" />'s <see cref="GalleryLayoutOptions.LayoutScale" />.
    /// </summary>
    private double _scale = 1;

    /// <summary>
    ///     The bounds of every item, in item order. <see cref="Calculate" /> and <see cref="Clear" /> change the list
    ///     in place, so read it again after either.
    /// </summary>
    public IReadOnlyList<Rect> Bounds => _bounds;

    /// <summary>
    ///     The size the panel measures to. Its scroll viewer scrolls over this and the panel's margin.
    /// </summary>
    public Size Extent { get; private set; }

    /// <summary>
    ///     Whether the last <see cref="Calculate" /> stacked the items in a single column, so the scroll axis is Y.
    /// </summary>
    public bool ScrollsVertically { get; private set; }

    /// <summary>
    ///     Whether a panel with these properties stacks its items in a single column, so its scroll axis is Y. That is
    ///     the vertical Docked Gallery; the horizontal one and the Expanded Gallery's columns scroll along X.
    /// </summary>
    public static bool ScrollsVerticallyFor(Orientation orientation, bool isExpanded)
        => !isExpanded && orientation == Orientation.Vertical;

    /// <summary>
    ///     Places every item, replacing the earlier bounds and extent.
    /// </summary>
    /// <param name="items">
    ///     The items in order. Each is <see cref="GalleryLayoutOptions.ItemWidth" /> wide when that is set. Otherwise a
    ///     <see cref="GalleryItemViewModel" /> that has its details takes its width from their aspect ratio, and any
    ///     other item is a square. Each width is rounded to whole device pixels.
    /// </param>
    /// <param name="options">The panel's layout properties.</param>
    /// <param name="availableSize">The size the panel was given.</param>
    public void Calculate(IReadOnlyList<object?> items, GalleryLayoutOptions options, Size availableSize)
    {
        _bounds.Clear();
        ScrollsVertically = ScrollsVerticallyFor(options.Orientation, options.IsExpanded);
        _scale = options.LayoutScale > 0 && double.IsFinite(options.LayoutScale) ? options.LayoutScale : 1;
        var itemSpacing = Snap(options.ItemSpacing);
        var lineSpacing = Snap(options.LineSpacing);

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

            itemWidth = Snap(itemWidth);

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
                    currentX += currentColumnMaxWidth + lineSpacing;
                    currentColumnMaxWidth = 0; // Reset for the new column
                    currentColumnStartIndex = _bounds.Count;
                }

                _bounds.Add(new Rect(currentX, currentY, itemWidth, options.ItemHeight));
                currentY += options.ItemHeight + itemSpacing;
                currentColumnMaxWidth = Math.Max(currentColumnMaxWidth, itemWidth);
                maxExtentY = Math.Max(maxExtentY, currentY - itemSpacing);
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
        _scale = 1;
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
    ///     The scroll offset that puts the item in the middle of a view, clamped to what the view can scroll over: the
    ///     extent and the panel's margin around it. It is on whole device pixels, a fraction of a pixel off the exact
    ///     middle at most, and moves by exactly as much as the item when sizes read before it move it.
    /// </summary>
    /// <param name="index">An index into <see cref="Bounds" />.</param>
    /// <param name="viewportLength">The length of the view along the scroll axis.</param>
    /// <param name="marginBefore">The panel's margin before its items along the scroll axis.</param>
    /// <param name="marginAfter">The panel's margin after its items along the scroll axis.</param>
    public double CenterOffset(int index, double viewportLength, double marginBefore = 0, double marginAfter = 0)
        => HoldOffsetOnDevicePixels(index, 0, viewportLength, marginBefore, marginAfter);

    /// <summary>
    ///     The scroll offset that puts the item at a distance from the middle of a view, clamped to what the view can
    ///     scroll over: the extent and the panel's margin around it. It is exact, not rounded to device pixels: a held
    ///     view is where the user scrolled, often between pixels, and the panel tells from the exact offset whether the
    ///     view has to move at all. <see cref="HoldOffsetOnDevicePixels" /> is where it moves when it does.
    /// </summary>
    /// <param name="index">An index into <see cref="Bounds" />.</param>
    /// <param name="distanceFromMiddle">How far the item's middle is past the middle of the view. Negative when before it.</param>
    /// <param name="viewportLength">The length of the view along the scroll axis.</param>
    /// <param name="marginBefore">The panel's margin before its items along the scroll axis.</param>
    /// <param name="marginAfter">The panel's margin after its items along the scroll axis.</param>
    public double HoldOffset(int index, double distanceFromMiddle, double viewportLength, double marginBefore = 0,
        double marginAfter = 0)
        => Clamp(marginBefore + Middle(index) - distanceFromMiddle - viewportLength / 2, viewportLength, marginBefore,
            marginAfter);

    /// <summary>
    ///     <see cref="HoldOffset" /> on whole device pixels, a fraction of a pixel off it at most. It moves by exactly as
    ///     much as the item when sizes read before it move it, and its rounding never adds up over many changes of the
    ///     view's length, as a window resized a pixel at a time makes, since the item's distance stays exact.
    /// </summary>
    /// <inheritdoc cref="HoldOffset" />
    public double HoldOffsetOnDevicePixels(int index, double distanceFromMiddle, double viewportLength,
        double marginBefore = 0, double marginAfter = 0)
    {
        // The item starts on a device pixel, unless it is in a side-docked strip whose item height is not a whole
        // number of them, so only what holding adds to its start is rounded. Rounding the sum would round a half pixel
        // up or down by the start's parity
        var bounds = _bounds[index];
        var (start, length) = ScrollsVertically ? (bounds.Y, bounds.Height) : (bounds.X, bounds.Width);
        return Clamp(start + Snap(marginBefore + length / 2 - distanceFromMiddle - viewportLength / 2),
            viewportLength, marginBefore, marginAfter);
    }

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
    ///     Where a view at <paramref name="offset" /> moves to on its way to <paramref name="target" />, on whole device
    ///     pixels: the target itself when it is on them, or else the pixel the view shows now moved by the whole device
    ///     pixels nearest the way there. A held view that the user left between pixels then moves by exactly as much
    ///     as the items do, when they move by whole device pixels.
    /// </summary>
    /// <param name="offset">The view's offset along the scroll axis now.</param>
    /// <param name="target">The offset the view should be at.</param>
    public double OnDevicePixels(double offset, double target)
    {
        if (IsOnDevicePixel(target))
        {
            return Snap(target);
        }

        // Layout shows an offset between pixels at its rounded pixel
        return Snap(offset) + Snap(target - offset);
    }

    /// <summary>
    ///     Whether an offset or position is on a whole device pixel, where layout rounding leaves it as it is.
    /// </summary>
    public bool IsOnDevicePixel(double value) => Math.Abs(Snap(value) - value) * _scale <= OnPixelTolerance;

    /// <summary>
    ///     Whether a view at <paramref name="other" /> shows another device pixel than a view at
    ///     <paramref name="offset" />. Layout shows each at its offset rounded to whole device pixels.
    /// </summary>
    public bool ShowsAnotherPixel(double offset, double other) => Math.Abs(Snap(other) - Snap(offset)) * _scale >= 0.5;

    /// <summary>
    ///     The value rounded to whole device pixels, as layout rounding rounds a position.
    /// </summary>
    private double Snap(double value) => LayoutHelper.RoundLayoutValue(value, _scale);

    /// <summary>
    ///     Keeps an offset within what the view can scroll to: from the start of the margin before the items to the end
    ///     of the margin after them. The end is on whole device pixels, and moves by as much as the extent does.
    /// </summary>
    private double Clamp(double offset, double viewportLength, double marginBefore, double marginAfter)
    {
        var extentLength = ScrollsVertically ? Extent.Height : Extent.Width;
        return Math.Clamp(offset, 0,
            Math.Max(0, extentLength + Snap(marginBefore + marginAfter - viewportLength)));
    }
}
