using Avalonia;
using PicView.Core.Gallery;

namespace PicView.Avalonia.CustomControls;

/// <summary>
///     Places the Expanded Gallery's Rows: rows of one height each, the items at their clamped aspect ratio. A row
///     that an item would make wider than the wrap width takes that item and is scaled down to end exactly at the
///     width; the last row, not filled, keeps the target height. All maths is in whole device pixels and divided by
///     the scale only when a <see cref="Rect" /> is made, so every edge is on a device pixel and an item's top never
///     decreases with its index.
/// </summary>
internal static class RowsPlacement
{
    /// <summary>
    ///     Adds every item's bounds to <paramref name="bounds" />, which is empty on entry, so an item's index is its
    ///     index there. Each item's aspect ratio waits in its <see cref="Rect" /> until its row is closed, so nothing is
    ///     allocated and each item's details are read once.
    /// </summary>
    /// <param name="items">The items in order.</param>
    /// <param name="options">The wrap width, item size, spacings and scale.</param>
    /// <param name="bounds">Receives the bounds in item order.</param>
    /// <returns>The extent: the wrap width, or the one row without a usable one, by the bottom of the last row.</returns>
    internal static Size Place(IReadOnlyList<object?> items, GalleryPlacementOptions options, List<Rect> bounds)
    {
        var scale = options.Scale;
        var targetHeight = Math.Max(1, options.InDevicePixels(options.ItemSize * GalleryDefaults.RowHeightFactor));
        var itemGap = options.InDevicePixels(options.ItemSpacing);
        var rowGap = options.InDevicePixels(options.LineSpacing);
        // Without a usable width nothing is ever wider than it: one row at the target height
        var width = options.HasUsableWidth ? options.WidthInDevicePixels : double.PositiveInfinity;

        double top = 0;
        double ratioSum = 0;
        var rowStart = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var ratio = GalleryPlacementOptions.AspectRatioOf(items[i]);
            bounds.Add(new Rect(0, 0, ratio, 0)); // the ratio waits in the width until CloseRow places the item
            ratioSum += ratio;
            var gaps = (i - rowStart) * itemGap;
            if (ratioSum * targetHeight + gaps <= width)
            {
                continue;
            }

            // Wider than the width with this item in it: the row is scaled down to end exactly there
            var exactHeight = (width - gaps) / ratioSum;
            var height = Math.Max(1, Math.Round(exactHeight));
            CloseRow(bounds, rowStart, i, top, exactHeight, height, itemGap, scale, width);
            top += height + rowGap;
            rowStart = i + 1;
            ratioSum = 0;
        }

        if (rowStart == items.Count)
        {
            return new Size(options.HasUsableWidth ? options.Width : 0, Math.Max(0, top - rowGap) / scale);
        }

        // The last row is not filled: it keeps the target height and starts at the left
        var right = CloseRow(bounds, rowStart, items.Count - 1, top, targetHeight, targetHeight, itemGap, scale,
            double.NaN);
        return new Size(options.HasUsableWidth ? options.Width : right / scale, (top + targetHeight) / scale);
    }

    /// <summary>Places a row's items, all in whole device pixels, and returns its right edge.</summary>
    /// <param name="bounds">The bounds, whose row items still hold their aspect ratio as their width.</param>
    /// <param name="first">The index of the row's first item.</param>
    /// <param name="last">The index of the row's last item.</param>
    /// <param name="top">The row's top, in device pixels.</param>
    /// <param name="exactHeight">The row's unrounded height, which the widths come from.</param>
    /// <param name="height">The row's height in whole device pixels.</param>
    /// <param name="itemGap">The gap between items, in device pixels.</param>
    /// <param name="scale">Device pixels per DIP.</param>
    /// <param name="end">Where the last item ends, or NaN to leave the row as wide as its items.</param>
    private static double CloseRow(List<Rect> bounds, int first, int last, double top, double exactHeight,
        double height, double itemGap, double scale, double end)
    {
        double left = 0;
        double right = 0;
        for (var i = first; i <= last; i++)
        {
            var width = Math.Max(1, Math.Round(bounds[i].Width * exactHeight));
            if (i == last && !double.IsNaN(end))
            {
                width = Math.Max(1, end - left); // the last item takes what rounding left over
            }

            bounds[i] = new Rect(left / scale, top / scale, width / scale, height / scale);
            right = left + width;
            left = right + itemGap;
        }

        return right;
    }
}
