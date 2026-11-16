using Avalonia;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

public class GalleryNavigatorTests
{
    /// <summary>
    /// A masonry of three columns, 100 wide and 10 apart, in the order the shortest-column rule fills them.
    /// Column 0 holds items 0 and 4, column 1 holds 1, 3, 5 and 7, column 2 holds 2 and 6.
    /// </summary>
    private static readonly Rect[] MasonryBounds =
    [
        new(0, 0, 100, 100),     // 0: column 0, from 0 to 100
        new(110, 0, 100, 60),    // 1: column 1, from 0 to 60
        new(220, 0, 100, 200),   // 2: column 2, from 0 to 200
        new(110, 70, 100, 60),   // 3: column 1, from 70 to 130
        new(0, 110, 100, 150),   // 4: column 0, from 110 to 260
        new(110, 140, 100, 100), // 5: column 1, from 140 to 240
        new(220, 210, 100, 50),  // 6: column 2, from 210 to 260
        new(110, 250, 100, 40)   // 7: column 1, from 250 to 290
    ];

    /// <summary>
    /// Three justified rows, 500 wide and 10 apart. Row 0 holds items 0 to 2, row 1 holds 3 to 5, and the last row
    /// holds 6 and 7 and ends at 310.
    /// </summary>
    private static readonly Rect[] RowsBounds =
    [
        new(0, 0, 150, 100),     // 0: from 0 to 150
        new(160, 0, 150, 100),   // 1: from 160 to 310
        new(320, 0, 180, 100),   // 2: from 320 to 500
        new(0, 110, 200, 100),   // 3: from 0 to 200
        new(210, 110, 140, 100), // 4: from 210 to 350
        new(360, 110, 140, 100), // 5: from 360 to 500
        new(0, 220, 100, 100),   // 6: from 0 to 100
        new(110, 220, 200, 100)  // 7: from 110 to 310
    ];

    [Theory]
    // Down and up stay in the column
    [InlineData(0, NavigationDirection.Down, 4)]
    [InlineData(1, NavigationDirection.Down, 3)]
    [InlineData(3, NavigationDirection.Down, 5)]
    [InlineData(5, NavigationDirection.Down, 7)]
    [InlineData(2, NavigationDirection.Down, 6)]
    [InlineData(4, NavigationDirection.Up, 0)]
    [InlineData(3, NavigationDirection.Up, 1)]
    [InlineData(7, NavigationDirection.Up, 5)]
    [InlineData(6, NavigationDirection.Up, 2)]
    // Left and right go to the next column's item that overlaps. Of several, the one whose center is nearest
    [InlineData(0, NavigationDirection.Right, 1)] // 1 and 3 overlap: 1's center is 20 away, 3's is 50
    [InlineData(4, NavigationDirection.Right, 5)] // 3, 5 and 7 overlap: 5's center is 5 away
    [InlineData(1, NavigationDirection.Right, 2)]
    [InlineData(5, NavigationDirection.Right, 6)] // 2 and 6 overlap: 6's center is 45 away, 2's is 90
    [InlineData(7, NavigationDirection.Right, 6)]
    [InlineData(2, NavigationDirection.Left, 3)]  // 1, 3 and 5 overlap: 3's center is level
    [InlineData(6, NavigationDirection.Left, 7)]  // 5 and 7 overlap: 7's center is 35 away, 5's is 45
    [InlineData(3, NavigationDirection.Left, 0)]  // 0 and 4 overlap: 0's center is 50 away, 4's is 85
    [InlineData(5, NavigationDirection.Left, 4)]
    [InlineData(7, NavigationDirection.Left, 4)]
    // The edges have no neighbor, and nothing wraps
    [InlineData(4, NavigationDirection.Down, -1)]
    [InlineData(7, NavigationDirection.Down, -1)]
    [InlineData(6, NavigationDirection.Down, -1)]
    [InlineData(0, NavigationDirection.Up, -1)]
    [InlineData(1, NavigationDirection.Up, -1)]
    [InlineData(2, NavigationDirection.Right, -1)]
    [InlineData(6, NavigationDirection.Right, -1)]
    [InlineData(0, NavigationDirection.Left, -1)]
    [InlineData(4, NavigationDirection.Left, -1)]
    public void Neighbor_Masonry_IsTheNearestOverlappingItem(int from, NavigationDirection direction, int expected) =>
        Assert.Equal(expected, GalleryNavigator.Neighbor(MasonryBounds, from, direction, GalleryDisplayMode.Masonry));

    [Theory]
    [InlineData(0, NavigationDirection.Right, 1)]
    [InlineData(2, NavigationDirection.Right, 3)] // across the end of row 0
    [InlineData(5, NavigationDirection.Right, 6)] // across the end of row 1
    [InlineData(7, NavigationDirection.Right, -1)]
    [InlineData(1, NavigationDirection.Left, 0)]
    [InlineData(3, NavigationDirection.Left, 2)]
    [InlineData(6, NavigationDirection.Left, 5)]
    [InlineData(0, NavigationDirection.Left, -1)]
    public void Neighbor_RowsLeftAndRight_FollowReadingOrder(int from, NavigationDirection direction, int expected) =>
        Assert.Equal(expected, GalleryNavigator.Neighbor(RowsBounds, from, direction, GalleryDisplayMode.Rows));

    [Theory]
    [InlineData(0, NavigationDirection.Down, 3)]
    [InlineData(1, NavigationDirection.Down, 4)] // 3 and 4 overlap: 4's center is 45 away, 3's is 135
    [InlineData(2, NavigationDirection.Down, 5)]
    [InlineData(3, NavigationDirection.Down, 6)] // 6 and 7 overlap: 6's center is 50 away, 7's is 110
    [InlineData(4, NavigationDirection.Down, 7)]
    [InlineData(5, NavigationDirection.Down, -1)] // the last row ends at 310, so nothing is below item 5
    [InlineData(6, NavigationDirection.Down, -1)]
    [InlineData(7, NavigationDirection.Up, 4)]   // 3 and 4 overlap: 4's center is 70 away, 3's is 110
    [InlineData(6, NavigationDirection.Up, 3)]
    [InlineData(3, NavigationDirection.Up, 0)]
    [InlineData(4, NavigationDirection.Up, 1)]
    [InlineData(5, NavigationDirection.Up, 2)]
    [InlineData(1, NavigationDirection.Up, -1)]
    public void Neighbor_RowsUpAndDown_IsTheNearestOverlappingItem(int from, NavigationDirection direction, int expected) =>
        Assert.Equal(expected, GalleryNavigator.Neighbor(RowsBounds, from, direction, GalleryDisplayMode.Rows));

    [Theory]
    [InlineData(GalleryDisplayMode.Masonry)]
    [InlineData(GalleryDisplayMode.Columns)]
    public void Neighbor_LeftAndRightOutsideRows_DoNotCrossRowEnds(GalleryDisplayMode displayMode)
    {
        Assert.Equal(-1, GalleryNavigator.Neighbor(RowsBounds, 2, NavigationDirection.Right, displayMode));
        Assert.Equal(-1, GalleryNavigator.Neighbor(RowsBounds, 3, NavigationDirection.Left, displayMode));
        Assert.Equal(1, GalleryNavigator.Neighbor(RowsBounds, 0, NavigationDirection.Right, displayMode));
    }

    [Theory]
    [InlineData(99.7, 1)]  // overlaps the current item by 0.3, which rounding can do: still below it
    [InlineData(99.4, -1)] // overlaps it by 0.6: not below it
    public void Neighbor_ItemOverTheEdge_CountsUpToHalfAPixel(double top, int expected)
    {
        Rect[] bounds = [new(0, 0, 100, 100), new(0, top, 100, 100)];
        Assert.Equal(expected, GalleryNavigator.Neighbor(bounds, 0, NavigationDirection.Down, GalleryDisplayMode.Masonry));
    }

    [Theory]
    [InlineData(100.4, 2)] // 0.4 further counts as equally near, so the item level with the center wins
    [InlineData(100.6, 1)] // 0.6 further is further, so the nearer item wins
    public void Neighbor_GapsWithinHalfAPixel_CountAsEquallyNear(double left, int expected)
    {
        Rect[] bounds = [new(0, 0, 100, 200), new(100, 0, 100, 60), new(left, 70, 100, 60)];
        Assert.Equal(expected, GalleryNavigator.Neighbor(bounds, 0, NavigationDirection.Right, GalleryDisplayMode.Masonry));
    }

    [Fact]
    public void Neighbor_TwoItemsEquallyNearAndCentered_IsTheLowerIndex()
    {
        // The centers of items 1 and 2 are both 55 from the center of item 0
        Rect[] bounds = [new(0, 0, 100, 210), new(110, 0, 100, 100), new(110, 110, 100, 100)];
        Assert.Equal(1, GalleryNavigator.Neighbor(bounds, 0, NavigationDirection.Right, GalleryDisplayMode.Masonry));
    }

    [Theory]
    [InlineData(NavigationDirection.Next)]
    [InlineData(NavigationDirection.Previous)]
    [InlineData(NavigationDirection.First)]
    [InlineData(NavigationDirection.Last)]
    [InlineData(NavigationDirection.PageUp)]
    [InlineData(NavigationDirection.PageDown)]
    public void Neighbor_DirectionThatIsNotAnArrow_HasNone(NavigationDirection direction)
    {
        Assert.Equal(-1, GalleryNavigator.Neighbor(MasonryBounds, 3, direction, GalleryDisplayMode.Masonry));
        Assert.Equal(-1, GalleryNavigator.Neighbor(RowsBounds, 3, direction, GalleryDisplayMode.Rows));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void Neighbor_FromOutsideTheBounds_HasNone(int from)
    {
        Assert.Equal(-1, GalleryNavigator.Neighbor(RowsBounds, from, NavigationDirection.Right, GalleryDisplayMode.Rows));
        Assert.Equal(-1, GalleryNavigator.Neighbor(RowsBounds, from, NavigationDirection.Left, GalleryDisplayMode.Rows));
        Assert.Equal(-1, GalleryNavigator.Neighbor(MasonryBounds, from, NavigationDirection.Down, GalleryDisplayMode.Masonry));
    }

    [Fact]
    public void Neighbor_NoBounds_HasNone() =>
        Assert.Equal(-1, GalleryNavigator.Neighbor(Array.Empty<Rect>(), 0, NavigationDirection.Right, GalleryDisplayMode.Rows));

    [Fact]
    public void Neighbor_CalledPerKeyPress_DoesNotAllocate()
    {
        IReadOnlyList<Rect> bounds = MasonryBounds;
        GalleryNavigator.Neighbor(bounds, 4, NavigationDirection.Right, GalleryDisplayMode.Masonry);
        GalleryNavigator.Neighbor(bounds, 4, NavigationDirection.Right, GalleryDisplayMode.Rows);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            GalleryNavigator.Neighbor(bounds, 4, NavigationDirection.Right, GalleryDisplayMode.Masonry);
            GalleryNavigator.Neighbor(bounds, 4, NavigationDirection.Up, GalleryDisplayMode.Rows);
            GalleryNavigator.Neighbor(bounds, 4, NavigationDirection.Left, GalleryDisplayMode.Rows);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
