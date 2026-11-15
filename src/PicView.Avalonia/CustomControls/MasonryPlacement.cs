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
    ///     Adds every item's bounds to <paramref name="bounds" />, which is empty on entry.
    /// </summary>
    /// <param name="items">The items in order.</param>
    /// <param name="options">The wrap width, item size, spacings and scale.</param>
    /// <param name="bounds">Receives the bounds in item order.</param>
    /// <param name="columnHeights">
    ///     The caller's buffer for the columns' heights. It is replaced only when it is shorter than the column count.
    /// </param>
    /// <returns>The extent: the wrap width, or one column without a usable one, by the tallest column.</returns>
    internal static Size Place(IReadOnlyList<object?> items, GalleryPlacementOptions options, List<Rect> bounds,
        ref double[] columnHeights)
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

        for (var i = 0; i < items.Count; i++)
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

            var height = Math.Max(1, Math.Round(columnWidth / GalleryPlacementOptions.AspectRatioOf(items[i])));
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
}
