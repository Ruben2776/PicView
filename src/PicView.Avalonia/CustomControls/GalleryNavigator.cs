using Avalonia;
using Avalonia.Input;
using PicView.Core.Gallery;

namespace PicView.Avalonia.CustomControls;

/// <summary>
///     Which Gallery Item an arrow key moves to in the Expanded Gallery's Masonry and Rows display modes.
/// </summary>
public static class GalleryNavigator
{
    /// <summary>
    ///     How far two edges can be apart, in DIPs, and still count as the same: an item can overlap the edge of the
    ///     current one by this much and still lie in the direction, and two gaps this close are equally near.
    /// </summary>
    private const double Tolerance = 0.5;

    /// <summary>
    ///     The item an arrow key moves to: the nearest item in the direction that overlaps the current one on the
    ///     other axis, the better centered of two equally near, the lower index when those tie too. Rows' left and
    ///     right are the item before and after. Nothing wraps. Returns -1 for none.
    /// </summary>
    public static int Neighbor(IReadOnlyList<Rect> bounds, int from, NavigationDirection direction,
        GalleryDisplayMode displayMode)
    {
        if (from < 0 || from >= bounds.Count ||
            direction is not (NavigationDirection.Left or NavigationDirection.Right or NavigationDirection.Up
                or NavigationDirection.Down))
        {
            return -1;
        }

        if (displayMode is GalleryDisplayMode.Rows &&
            direction is (NavigationDirection.Left or NavigationDirection.Right))
        {
            // Reading order, so the ends of a row lead to the row before or after
            var next = direction is NavigationDirection.Left ? from - 1 : from + 1;
            return next >= 0 && next < bounds.Count ? next : -1;
        }

        var vertical = direction is NavigationDirection.Up or NavigationDirection.Down;
        var current = bounds[from];
        var best = -1;
        var smallestGap = double.MaxValue;
        var bestCenterDistance = double.MaxValue;

        for (var i = 0; i < bounds.Count; i++)
        {
            if (i == from)
            {
                continue;
            }

            var rect = bounds[i];
            // From the current item's edge in the direction to the other item's facing edge
            var gap = direction switch
            {
                NavigationDirection.Up => current.Top - rect.Bottom,
                NavigationDirection.Down => rect.Top - current.Bottom,
                NavigationDirection.Left => current.Left - rect.Right,
                _ => rect.Left - current.Right
            };
            // How much the two share on the other axis
            var overlap = vertical
                ? Math.Min(rect.Right, current.Right) - Math.Max(rect.Left, current.Left)
                : Math.Min(rect.Bottom, current.Bottom) - Math.Max(rect.Top, current.Top);
            if (gap < -Tolerance || overlap <= 0)
            {
                continue;
            }

            var centerDistance = vertical
                ? Math.Abs(rect.Center.X - current.Center.X)
                : Math.Abs(rect.Center.Y - current.Center.Y);
            if (gap < smallestGap - Tolerance ||
                (Math.Abs(gap - smallestGap) <= Tolerance && centerDistance < bestCenterDistance))
            {
                best = i;
                bestCenterDistance = centerDistance;
            }

            smallestGap = Math.Min(smallestGap, gap);
        }

        return best;
    }
}
