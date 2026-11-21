using Avalonia;
using PicView.Core.Gallery;

namespace PicView.Avalonia.CustomControls;

/// <summary>
///     Places the Expanded Gallery's Masonry: columns of one width, each item in the shortest column at its clamped
///     aspect ratio. All maths is in whole device pixels and divided by the scale only when a <see cref="Rect" /> is
///     made, so every edge is on a device pixel and an item's top never decreases with its index.
/// </summary>
internal static class MasonryPlacement
{
    /// <summary>
    ///     Leaves every item's bounds in <paramref name="bounds" />.
    /// </summary>
    /// <param name="items">The items in order.</param>
    /// <param name="options">The wrap width, item size, spacings and scale.</param>
    /// <param name="bounds">
    ///     Receives the bounds in item order. It is empty on entry, or holds what an earlier call with the same
    ///     <paramref name="options" /> left when <paramref name="keepPlaced" /> is set.
    /// </param>
    /// <param name="columnHeights">
    ///     The caller's buffer for the columns' heights. It is replaced only when it is shorter than the column count.
    /// </param>
    /// <param name="keepPlaced">
    ///     Whether <paramref name="bounds" /> holds an earlier placement with the same <paramref name="options" />. The
    ///     items before the first whose height changed then keep their bounds, since an item's place depends only on
    ///     the heights before it, and placing starts at that item.
    /// </param>
    /// <returns>The extent: the wrap width, or one column without a usable one, by the tallest column.</returns>
    internal static Size Place(IReadOnlyList<object?> items, GalleryPlacementOptions options, List<Rect> bounds,
        ref double[] columnHeights, bool keepPlaced = false)
    {
        var scale = options.Scale;
        var columnGap = options.InDevicePixels(options.LineSpacing);
        var itemGap = options.InDevicePixels(options.ItemSpacing);
        var minWidth = Math.Max(1, options.ItemSize * GalleryDefaults.MasonryColumnWidthFactor * scale);

        // Without a usable width: one column of the minimum width
        var count = 1;
        var columnWidth = Math.Max(1, Math.Floor(minWidth + GalleryPlacementOptions.PixelTolerance));
        if (options.HasUsableWidth)
        {
            var width = options.WidthInDevicePixels;
            count = Math.Max(1,
                (int)Math.Floor((width + columnGap) / (minWidth + columnGap) + GalleryPlacementOptions.PixelTolerance));
            columnWidth = Math.Max(1, Math.Floor((width - (count - 1) * columnGap) / count));
        }

        if (columnHeights.Length < count)
        {
            columnHeights = new double[count];
        }

        var heights = columnHeights.AsSpan(0, count);
        heights.Clear();

        var first = 0;
        if (keepPlaced)
        {
            // Bounds are whole device pixels divided by the scale, so rounding gives the placed height back exactly
            var placed = Math.Min(items.Count, bounds.Count);
            while (first < placed && HeightOf(items[first], columnWidth) == Math.Round(bounds[first].Height * scale))
            {
                first++;
            }

            RestoreColumnHeights(bounds, first, heights, columnWidth + columnGap, itemGap, scale);
        }

        if (first < bounds.Count)
        {
            bounds.RemoveRange(first, bounds.Count - first);
        }

        for (var i = first; i < items.Count; i++)
        {
            // Heights are whole device pixels, so "shorter" is exact and the leftmost wins a tie
            var column = 0;
            for (var c = 1; c < count; c++)
            {
                if (heights[c] < heights[column])
                {
                    column = c;
                }
            }

            var height = HeightOf(items[i], columnWidth);
            bounds.Add(new Rect(column * (columnWidth + columnGap) / scale, heights[column] / scale,
                columnWidth / scale, height / scale));
            heights[column] += height + itemGap;
        }

        double tallest = 0;
        foreach (var columnHeight in heights)
        {
            tallest = Math.Max(tallest, columnHeight);
        }

        return new Size(options.HasUsableWidth ? options.Width : columnWidth / scale,
            Math.Max(0, tallest - itemGap) / scale);
    }

    /// <summary>The item's height in a column of <paramref name="columnWidth" />, both in whole device pixels.</summary>
    private static double HeightOf(object? item, double columnWidth) =>
        Math.Max(1, Math.Round(columnWidth / GalleryPlacementOptions.AspectRatioOf(item)));

    /// <summary>
    ///     Sets the columns' heights to what they were once the first <paramref name="count" /> items were placed: each
    ///     column ends under its last item. It walks back from the last of them until every column has its item, which
    ///     is a few items back, since each item goes to the shortest column.
    /// </summary>
    /// <param name="bounds">The earlier placement.</param>
    /// <param name="count">How many items from the start are kept.</param>
    /// <param name="heights">The columns' heights, all zero on entry, as a column without an item stays.</param>
    /// <param name="columnPitch">A column's width and the gap after it, in device pixels.</param>
    /// <param name="itemGap">The gap under an item, in device pixels.</param>
    /// <param name="scale">Device pixels per DIP.</param>
    private static void RestoreColumnHeights(List<Rect> bounds, int count, Span<double> heights, double columnPitch,
        double itemGap, double scale)
    {
        var missing = heights.Length;
        for (var i = count - 1; i >= 0 && missing > 0; i--)
        {
            // An item's height is at least one device pixel, so a column that has an item is never at zero
            var column = (int)Math.Round(bounds[i].X * scale / columnPitch);
            if (heights[column] > 0)
            {
                continue;
            }

            heights[column] = Math.Round(bounds[i].Bottom * scale) + itemGap;
            missing--;
        }
    }
}
