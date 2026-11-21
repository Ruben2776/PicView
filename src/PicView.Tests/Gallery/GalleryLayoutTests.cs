using Avalonia;
using Avalonia.Layout;
using PicView.Avalonia.CustomControls;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Tests.Gallery;

public class GalleryLayoutTests
{
    private static readonly GalleryLayoutOptions Docked =
        new(Orientation.Horizontal, IsExpanded: false, ItemHeight: 100, ItemWidth: double.NaN, 0, 0, double.NaN);

    private static GalleryItemViewModel Item(uint width, uint height)
    {
        var item = new GalleryItemViewModel();
        item.SetDetails(new GalleryFileDetails(width, height));
        return item;
    }

    private static object?[] Squares(int count) => Enumerable.Range(0, count).Select(_ => (object?)Item(100, 100)).ToArray();

    /// <summary>
    /// DIPs as device pixels at <paramref name="scale"/>, to six decimals, which drops the rounding errors of DIPs that
    /// are not whole numbers.
    /// </summary>
    private static double DevicePixels(double dips, double scale) => Math.Round(dips * scale, 6);

    private static double[] DevicePixels(IEnumerable<double> dips, double scale) =>
        dips.Select(value => DevicePixels(value, scale)).ToArray();

    // Item size 75: a Masonry column is at least 100 wide (75 × 4/3) and the Rows target height is 150 (75 × 2)
    private static readonly GalleryLayoutOptions Masonry = new(Orientation.Vertical, IsExpanded: true, ItemHeight: 75,
        ItemWidth: double.NaN, ItemSpacing: 10, LineSpacing: 10, WrapHeightOverride: double.NaN,
        DisplayMode: GalleryDisplayMode.Masonry);

    private static readonly GalleryLayoutOptions Rows = Masonry with { DisplayMode = GalleryDisplayMode.Rows };

    private static object?[] SevenTiles() =>
    [
        Item(100, 100), Item(200, 100), Item(100, 200), Item(100, 100), Item(100, 100), new GalleryItemViewModel(),
        Item(100, 100)
    ];

    /// <summary>Sizes from 200 to 3990 a side, the same every run, with ratios past both ends of the tile clamp.</summary>
    private static object?[] Varied(int count) => Enumerable.Range(0, count)
        .Select(i => (object?)Item((uint)(200 + i * 37 % 380 * 10), (uint)(200 + i * 91 % 380 * 10))).ToArray();

    private static void AssertOnDevicePixels(IReadOnlyList<Rect> bounds, double scale)
    {
        foreach (var rect in bounds)
        {
            double[] edges = [rect.X, rect.Y, rect.Right, rect.Bottom];
            Assert.All(edges, edge => Assert.Equal(Math.Round(DevicePixels(edge, scale)), DevicePixels(edge, scale)));
        }
    }

    [Fact]
    public void Calculate_HorizontalDocked_PlacesItemsSideBySideAtTheirAspectRatio()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(200, 100), Item(100, 100), new GalleryItemViewModel()], Docked, new Size(1000, 100));
        Assert.Equal([new Rect(0, 0, 200, 100), new Rect(200, 0, 100, 100), new Rect(300, 0, 100, 100)], layout.Bounds);
        Assert.Equal(new Size(400, 100), layout.Extent);
    }

    [Fact]
    public void Calculate_VerticalDocked_StacksItemsAtTheFullWidth()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(2), Docked with { Orientation = Orientation.Vertical }, new Size(150, 1000));
        Assert.Equal([new Rect(0, 0, 150, 100), new Rect(0, 100, 150, 100)], layout.Bounds);
        Assert.True(layout.ScrollsVertically);
    }

    [Fact]
    public void Calculate_Expanded_WrapsIntoColumnsAsWideAsTheirWidestItem()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(200, 100), Item(100, 100), Item(100, 100)],
            Docked with { IsExpanded = true, ItemSpacing = 10, LineSpacing = 5 }, new Size(1000, 250));
        Assert.Equal([new Rect(0, 0, 200, 100), new Rect(0, 110, 200, 100), new Rect(205, 0, 100, 100)], layout.Bounds);
    }

    [Theory]
    [InlineData(5, 400)] // middle 550
    [InlineData(0, 0)]   // clamped at the start
    [InlineData(9, 700)] // clamped at the end: 1000 − 300
    public void CenterOffset_TenSquaresInA300View_CentersOrClamps(int index, double expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal(expected, layout.CenterOffset(index, 300));
    }

    [Fact]
    public void CenterOffset_StripShorterThanTheView_IsZero()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(2), Docked, new Size(300, 100));
        Assert.Equal(0, layout.CenterOffset(1, 300));
    }

    [Fact]
    public void HoldOffset_WidthsChangeToTheLeft_MovesByTheSameAmount()
    {
        var items = Squares(10);
        var layout = new GalleryLayout();
        layout.Calculate(items, Docked, new Size(300, 100));
        foreach (var item in items[..5])
        {
            ((GalleryItemViewModel)item!).SetDetails(new GalleryFileDetails(200, 100));
        }
        layout.Calculate(items, Docked, new Size(300, 100));
        Assert.Equal(900, layout.HoldOffset(5, 0, 300)); // was 400, plus five items 100 wider
    }

    [Fact]
    public void HoldOffset_WidthsChangeToTheRight_KeepsTheOffset()
    {
        var items = Squares(10);
        var layout = new GalleryLayout();
        foreach (var item in items[6..])
        {
            ((GalleryItemViewModel)item!).SetDetails(new GalleryFileDetails(200, 100));
        }
        layout.Calculate(items, Docked, new Size(300, 100));
        Assert.Equal(400, layout.HoldOffset(5, 0, 300));
    }

    [Theory]
    [InlineData(550, 5)]
    [InlineData(-50, 0)]
    [InlineData(5000, 9)]
    public void IndexNearest_TenSquares_PicksTheItemWhoseMiddleIsNearest(double position, int expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal(expected, layout.IndexNearest(position));
    }

    [Fact]
    public void Calculate_ItemWidthSet_GivesEveryItemThatWidthWhateverItsDetails()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(200, 100), new GalleryItemViewModel()], Docked with { ItemWidth = 80 }, new Size(1000, 100));
        Assert.Equal([new Rect(0, 0, 80, 100), new Rect(80, 0, 80, 100)], layout.Bounds);
    }

    [Fact]
    public void Calculate_UnreadableDetails_PlacesASquare()
    {
        var unreadable = new GalleryItemViewModel();
        unreadable.SetDetails(GalleryFileDetails.Unreadable);
        var layout = new GalleryLayout();
        layout.Calculate([unreadable], Docked, new Size(1000, 100));
        Assert.Equal([new Rect(0, 0, 100, 100)], layout.Bounds);
    }

    [Theory]
    [InlineData(Orientation.Horizontal, false, false)]
    [InlineData(Orientation.Vertical, false, true)]
    [InlineData(Orientation.Horizontal, true, false)]
    [InlineData(Orientation.Vertical, true, false)] // the Expanded Gallery wraps into columns, so it scrolls sideways
    public void ScrollsVertically_ByOrientationAndMode_IsTrueOnlyInTheVerticalDockedGallery(
        Orientation orientation, bool isExpanded, bool expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(2), Docked with { Orientation = orientation, IsExpanded = isExpanded }, new Size(300, 300));
        Assert.Equal(expected, layout.ScrollsVertically);
    }

    [Theory]
    [InlineData(Orientation.Horizontal, false, GalleryDisplayMode.Columns, false)]
    [InlineData(Orientation.Vertical, false, GalleryDisplayMode.Columns, true)]
    [InlineData(Orientation.Horizontal, true, GalleryDisplayMode.Columns, false)]
    [InlineData(Orientation.Vertical, true, GalleryDisplayMode.Columns, false)] // Columns wraps, so it scrolls sideways
    [InlineData(Orientation.Vertical, true, GalleryDisplayMode.Masonry, true)]
    [InlineData(Orientation.Horizontal, true, GalleryDisplayMode.Masonry, true)]
    [InlineData(Orientation.Vertical, true, GalleryDisplayMode.Rows, true)]
    [InlineData(Orientation.Horizontal, false, GalleryDisplayMode.Masonry, false)] // the Docked Gallery has no display mode
    [InlineData(Orientation.Vertical, false, GalleryDisplayMode.Rows, true)]
    public void ScrollsVertically_ByOrientationModeAndDisplayMode_IsTrueInTheVerticalDockedGalleryAndInMasonryAndRows(
        Orientation orientation, bool isExpanded, GalleryDisplayMode displayMode, bool expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(2),
            Docked with { Orientation = orientation, IsExpanded = isExpanded, DisplayMode = displayMode },
            new Size(300, 300));
        Assert.Equal(expected, layout.ScrollsVertically);
        Assert.Equal(expected, GalleryLayout.ScrollsVerticallyFor(orientation, isExpanded, displayMode));
    }

    [Theory]
    [InlineData(5, 400)] // middle 550
    [InlineData(0, 0)]   // clamped at the start
    [InlineData(9, 700)] // clamped at the end: 1000 − 300
    public void CenterOffset_VerticalDockedTenSquaresInA300View_CentersOrClampsAlongTheHeight(int index, double expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked with { Orientation = Orientation.Vertical }, new Size(150, 300));
        Assert.Equal(expected, layout.CenterOffset(index, 300));
    }

    [Theory]
    [InlineData(40, 360)]    // item 5's middle, 550, is 40 past the middle of the view at 360, which is 510
    [InlineData(-40, 440)]   // and 40 before the middle of the view at 440, which is 590
    [InlineData(1000, 0)]    // clamped at the start
    [InlineData(-1000, 700)] // clamped at the end: 1000 − 300
    public void HoldOffset_ItemAwayFromTheMiddle_PutsItBackAtThatDistance(double distanceFromMiddle, double expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal(expected, layout.HoldOffset(5, distanceFromMiddle, 300));
    }

    // The scroll viewer scrolls over the panel's margin too, so the items start the margin before into it
    [Theory]
    [InlineData(5, 440)] // middle 550, after the margin before, 40
    [InlineData(0, 0)]   // clamped at the start of the margin before
    [InlineData(9, 745)] // clamped at the end of the margin after: 40 + 1000 + 5 − 300
    public void CenterOffset_TenSquaresWithMarginsInA300View_CentersOrClampsToTheMargins(int index, double expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal(expected, layout.CenterOffset(index, 300, marginBefore: 40, marginAfter: 5));
    }

    [Fact]
    public void CenterOffset_StripAndMarginsShorterThanTheView_IsZero()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(2), Docked, new Size(300, 100));
        Assert.Equal(0, layout.CenterOffset(1, 300, marginBefore: 40, marginAfter: 5)); // 40 + 200 + 5 is under 300
    }

    [Theory]
    [InlineData(40, 400)]    // item 5's middle, 40 + 550, is 40 past the middle of the view at 400, which is 550
    [InlineData(1000, 0)]    // clamped at the start of the margin before
    [InlineData(-1000, 745)] // clamped at the end of the margin after: 40 + 1000 + 5 − 300
    public void HoldOffset_WithMargins_PutsTheItemBackAtThatDistanceWithinTheMargins(double distanceFromMiddle,
        double expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal(expected, layout.HoldOffset(5, distanceFromMiddle, 300, marginBefore: 40, marginAfter: 5));
    }

    [Theory]
    [InlineData(599, 5)]
    [InlineData(600, 5)] // as near the middle of item 5, 550, as of item 6, 650: the lower index wins
    [InlineData(601, 6)]
    public void IndexNearest_PositionBetweenTwoMiddles_PicksTheNearerAndTheLowerOnATie(double position, int expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal(expected, layout.IndexNearest(position));
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(100, 0)]  // as near the first column's middle, 50, as the second's, 150: the lower index wins
    [InlineData(101, 3)]
    [InlineData(200, 3)]  // as near the second column's middle as the third's, 250
    [InlineData(5000, 6)]
    public void IndexNearest_ExpandedColumns_PicksTheFirstItemOfTheNearestColumn(double position, int expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(9), Docked with { IsExpanded = true }, new Size(1000, 300)); // three columns of three
        Assert.Equal(expected, layout.IndexNearest(position));
    }

    [Fact]
    public void IndicesIn_AreaOverTheMiddleOfAStrip_ReturnsTheFirstAndLastItemsItReaches()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal((2, 5), layout.IndicesIn(new Rect(250, 0, 300, 100)));
    }

    [Fact]
    public void IndicesIn_AreaMeetingItemsAtTheirEdges_LeavesThoseItemsOut()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal((3, 4), layout.IndicesIn(new Rect(300, 0, 200, 100))); // item 2 ends at 300, and item 5 starts at 500
    }

    [Fact]
    public void IndicesIn_NothingIntersects_ReturnsMinusOneMinusOne()
    {
        var layout = new GalleryLayout();
        Assert.Equal((-1, -1), layout.IndicesIn(new Rect(0, 0, 100, 100))); // no bounds yet
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal((-1, -1), layout.IndicesIn(new Rect(1000, 0, 100, 100))); // starts where item 9 ends
    }

    [Fact]
    public void IndicesIn_ExpandedColumns_ReturnsFromTheFirstItemToTheLastEvenWhenOnesBetweenMissTheArea()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(9), Docked with { IsExpanded = true }, new Size(1000, 300)); // three columns of three
        // The top of the first two columns: items 0 and 3 reach it, and items 1 and 2 below item 0 do not
        Assert.Equal((0, 3), layout.IndicesIn(new Rect(50, 0, 100, 50)));
    }

    // At 125% a device pixel is 0.8 DIP: 133.33 and 66.67 DIPs are 166.67 and 83.33 device pixels
    [Fact]
    public void Calculate_AtAFractionalScale_RoundsEveryWidthToWholeDevicePixels()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(4000, 3000), Item(2000, 3000), new GalleryItemViewModel()],
            Docked with { LayoutScale = 1.25 }, new Size(1000, 100));
        Assert.Equal<double>([0, 167, 250], DevicePixels(layout.Bounds.Select(bounds => bounds.X), 1.25));
        Assert.Equal<double>([167, 83, 125], DevicePixels(layout.Bounds.Select(bounds => bounds.Width), 1.25));
        Assert.Equal(375, DevicePixels(layout.Extent.Width, 1.25));
    }

    // Spacings of 5 DIPs are 6.25 device pixels at 125%, which layout rounds to 6
    [Fact]
    public void Calculate_ExpandedAtAFractionalScale_RoundsColumnWidthsAndSpacingsToWholeDevicePixels()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(4000, 3000), Item(2000, 3000), Item(1000, 1000)],
            Docked with { IsExpanded = true, ItemSpacing = 5, LineSpacing = 5, LayoutScale = 1.25 },
            new Size(1000, 250));
        Assert.Equal<double>([0, 0, 173], DevicePixels(layout.Bounds.Select(bounds => bounds.X), 1.25)); // 167 + 6
        Assert.Equal<double>([0, 131, 0], DevicePixels(layout.Bounds.Select(bounds => bounds.Y), 1.25)); // 125 + 6
        Assert.Equal<double>([167, 167, 125], DevicePixels(layout.Bounds.Select(bounds => bounds.Width), 1.25));
    }

    // At 125%, item 5 is 250 device pixels wide and the view 375, so the exact centered offset is half a pixel off the
    // device pixels. Rounding it whole would go up or down with the item's start
    [Fact]
    public void CenterOffset_AtAFractionalScaleWhenAnItemBeforeWidensByADevicePixel_MovesByExactlyThatPixel()
    {
        object?[] items = [new GalleryItemViewModel(), .. Enumerable.Range(0, 9).Select(_ => Item(2000, 1000))];
        var layout = new GalleryLayout();
        var options = Docked with { LayoutScale = 1.25 };
        layout.Calculate(items, options, new Size(300, 100));
        var centered = DevicePixels(layout.CenterOffset(5, 300), 1.25);

        ((GalleryItemViewModel)items[0]!).SetDetails(new GalleryFileDetails(1008, 1000)); // 126 device pixels, not 125
        layout.Calculate(items, options, new Size(300, 100));

        Assert.Equal(1063, centered); // the item's start, 1125, plus 125 − 187.5 rounded to even, −62
        Assert.Equal(1064, DevicePixels(layout.CenterOffset(5, 300), 1.25));
    }

    // Margins of 15 are 18.75 device pixels each at 125%, which would end what the view can scroll over between pixels
    [Fact]
    public void CenterOffset_ClampedAtTheEndWithMarginsAtAFractionalScale_IsOnAWholeDevicePixel()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked with { LayoutScale = 1.25 }, new Size(300, 100));
        // 1250 + 37.5 − 375 device pixels, with the margins and the view rounded to even
        Assert.Equal(912, DevicePixels(layout.CenterOffset(9, 300, marginBefore: 15, marginAfter: 15), 1.25));
    }

    // At 200%, a view at 62.75 is 125.5 device pixels in, which layout shows at the even pixel, 126
    [Theory]
    [InlineData(62.75, 70, 70)]      // a target on a device pixel is where the view goes
    [InlineData(62.75, 70.5, 70.5)]  // 141 is a pixel too; 15.5 pixels on from the 126 shown would round to 142
    [InlineData(62.75, 63.25, 63.5)] // 126.5 would show at 126 too; one pixel on from the 126 shown is 127
    [InlineData(62.75, 61.25, 61.5)] // 122.5 would show at 122; three pixels back from the 126 shown is 123
    public void OnDevicePixels_AtTwoHundredPercent_MovesWhatTheViewShowsByWholeDevicePixels(double offset,
        double target, double expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(100), Docked with { LayoutScale = 2 }, new Size(300, 100));
        Assert.Equal(expected, layout.OnDevicePixels(offset, target));
    }

    [Theory]
    [InlineData(100, 100.2, false)] // 200 and 200.4 device pixels show at 200
    [InlineData(100, 99.75, false)] // 199.5 shows at the even pixel, 200
    [InlineData(100, 100.5, true)]  // 201
    public void ShowsAnotherPixel_AtTwoHundredPercent_IsTrueOnlyWhenLayoutRoundsToAnotherDevicePixel(double offset,
        double other, bool expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked with { LayoutScale = 2 }, new Size(300, 100));
        Assert.Equal(expected, layout.ShowsAnotherPixel(offset, other));
    }

    [Fact]
    public void Clear_AfterCalculate_LeavesAnEmptyLayout()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(3), Docked with { Orientation = Orientation.Vertical }, new Size(150, 1000));

        layout.Clear();

        Assert.Empty(layout.Bounds);
        Assert.Equal(new Size(), layout.Extent);
        Assert.False(layout.ScrollsVertically);
        Assert.Equal(-1, layout.IndexNearest(0));
    }

    [Theory]
    [InlineData(GalleryDisplayMode.Masonry)]
    [InlineData(GalleryDisplayMode.Rows)]
    public void Calculate_DockedWithATileDisplayMode_PlacesTheStripAsBefore(GalleryDisplayMode displayMode)
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(200, 100), Item(100, 100)], Docked with { DisplayMode = displayMode }, new Size(1000, 100));
        Assert.Equal([new Rect(0, 0, 200, 100), new Rect(200, 0, 100, 100)], layout.Bounds);
        Assert.Equal(new Size(300, 100), layout.Extent);
    }

    [Fact]
    public void Calculate_Masonry_PlacesEachItemInTheShortestColumnAndTheLeftmostOnATie()
    {
        var layout = new GalleryLayout();
        layout.Calculate(SevenTiles(), Masonry, new Size(320, 500)); // three columns of 100 at 0, 110 and 220
        Assert.Equal(
        [
            new Rect(0, 0, 100, 100),     // every column is empty: the leftmost
            new Rect(110, 0, 100, 50),    // the second and third are empty: the left one. 2:1 is half as tall
            new Rect(220, 0, 100, 200),   // 1:2 is twice as tall
            new Rect(110, 60, 100, 100),  // the columns end at 110, 60 and 210
            new Rect(0, 110, 100, 100),   // 110, 170, 210
            new Rect(110, 170, 100, 100), // 220, 170, 210. An item not read yet is a square
            new Rect(220, 210, 100, 100)  // 220, 280, 210
        ], layout.Bounds);
        Assert.Equal(new Size(320, 310), layout.Extent); // the third column ends at 320, less the spacing under it
        Assert.True(layout.ScrollsVertically);
    }

    [Theory]
    [InlineData(99, 1, 99)]     // narrower than one column: still one
    [InlineData(209, 1, 209)]
    [InlineData(210, 2, 100)]   // 100 + 10 + 100
    [InlineData(317, 2, 153)]   // 153.5 each, down to the whole pixel
    [InlineData(320, 3, 100)]
    [InlineData(320.9, 3, 100)] // the part of a pixel is left over
    [InlineData(322, 3, 100)]   // 100.67 each: rounding up would end past the width
    [InlineData(430, 4, 100)]
    public void Calculate_MasonryByWidth_FitsColumnsOfAtLeastTheMinimumWidth(double width, int columns, double columnWidth)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(8), Masonry, new Size(width, 500));
        Assert.Equal(columns, layout.Bounds.Select(bounds => bounds.X).Distinct().Count());
        Assert.All(layout.Bounds, bounds => Assert.Equal(columnWidth, bounds.Width));
        Assert.Equal(width, layout.Extent.Width);
    }

    [Fact]
    public void Calculate_MasonryExtremeRatiosAndItemsWithoutASize_ClampsToOneThirdAndFourAndPlacesSquares()
    {
        var unreadable = new GalleryItemViewModel();
        unreadable.SetDetails(GalleryFileDetails.Unreadable);
        var layout = new GalleryLayout();
        layout.Calculate([Item(100, 2000), Item(4000, 100), new GalleryItemViewModel(), unreadable], Masonry,
            new Size(100, 500));
        Assert.Equal([new Rect(0, 0, 100, 300), new Rect(0, 310, 100, 25), new Rect(0, 345, 100, 100),
            new Rect(0, 455, 100, 100)], layout.Bounds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(double.PositiveInfinity)]
    public void Calculate_MasonryWithoutAUsableWidth_PlacesOneColumnOfTheMinimumWidth(double width)
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(200, 100), Item(100, 100)], Masonry, new Size(width, 500));
        Assert.Equal([new Rect(0, 0, 100, 50), new Rect(0, 60, 100, 100)], layout.Bounds);
        Assert.Equal(new Size(100, 160), layout.Extent);
    }

    [Theory]
    [InlineData(320, 3)]        // wraps at the override, not at the 1000 available
    [InlineData(double.NaN, 9)] // not set: (1000 + 10) / 110 columns
    [InlineData(0, 9)]
    public void Calculate_MasonryWrapWidthOverride_WrapsAtItWhenSet(double wrapWidth, int columns)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(12), Masonry with { WrapWidthOverride = wrapWidth }, new Size(1000, 500));
        Assert.Equal(columns, layout.Bounds.Select(bounds => bounds.X).Distinct().Count());
        Assert.Equal(wrapWidth > 0 ? wrapWidth : 1000, layout.Extent.Width);
    }

    // At 125%, 320 is 400 device pixels, a spacing of 10 is 12.5, which rounds to 12, and a column is at least 125:
    // three columns of (400 − 24) / 3, down to 125
    [Fact]
    public void Calculate_MasonryAtAFractionalScale_PutsColumnsGapsAndHeightsOnWholeDevicePixels()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(4000, 3000), Item(2000, 3000), Item(100, 100), Item(100, 100)],
            Masonry with { LayoutScale = 1.25 }, new Size(320, 500));
        Assert.Equal<double>([0, 137, 274, 0], DevicePixels(layout.Bounds.Select(bounds => bounds.X), 1.25));
        Assert.Equal<double>([0, 0, 0, 106], DevicePixels(layout.Bounds.Select(bounds => bounds.Y), 1.25)); // 94 + 12
        Assert.Equal<double>([125, 125, 125, 125], DevicePixels(layout.Bounds.Select(bounds => bounds.Width), 1.25));
        // 125 / (4/3) = 93.75 and 125 / (2/3) = 187.5
        Assert.Equal<double>([94, 188, 125, 125], DevicePixels(layout.Bounds.Select(bounds => bounds.Height), 1.25));
        Assert.Equal(231, DevicePixels(layout.Extent.Height, 1.25)); // 106 + 125
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    [InlineData(2.5)]
    public void Calculate_MasonryVariedItemsAtAnyScale_KeepsTopsFromDecreasingAndEveryEdgeOnADevicePixel(double scale)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Varied(300), Masonry with { LayoutScale = scale }, new Size(1000, 500));
        var bounds = layout.Bounds;

        Assert.Equal(9, bounds.Select(rect => rect.X).Distinct().Count());
        AssertOnDevicePixels(bounds, scale);
        Assert.All(bounds, rect => Assert.True(DevicePixels(rect.Right, scale) <= DevicePixels(1000, scale)));
        Assert.Equal(bounds.Max(rect => rect.Bottom), layout.Extent.Height, 6);
        for (var i = 1; i < bounds.Count; i++)
        {
            Assert.True(bounds[i].Y >= bounds[i - 1].Y, $"Item {i} starts above item {i - 1}");
        }

        for (var i = 0; i < bounds.Count; i++)
        {
            var found = layout.IndexNearest(bounds[i].Y); // the first item at that top
            Assert.True(found <= i && bounds[found].Y.Equals(bounds[i].Y), $"Item {i}'s top finds item {found}");
        }
    }

    // At 250% half a DIP is more than a device pixel. "Shorter" is compared in whole device pixels (the design's call,
    // departing from the fork's 0.5 DIP tolerance), so a column one device pixel shorter takes the next item
    [Fact]
    public void Calculate_MasonryColumnOneDevicePixelShorterAt250Percent_IsShorterNotATie()
    {
        var layout = new GalleryLayout();
        // 210 is 525 device pixels: two columns of 250, 25 apart. The first item is 251 high there, the second 250
        layout.Calculate([Item(250, 251), Item(100, 100), Item(100, 100), Item(100, 100)],
            Masonry with { LayoutScale = 2.5 }, new Size(210, 500));
        Assert.Equal<double>([0, 275, 275, 0], DevicePixels(layout.Bounds.Select(bounds => bounds.X), 2.5));
        // The columns end at 276 and 275. With the fork's tolerance the third item would go left, to 276, and the
        // fourth right, to 275: a top that decreases
        Assert.Equal<double>([0, 0, 275, 276], DevicePixels(layout.Bounds.Select(bounds => bounds.Y), 2.5));
    }

    // The design: LineSpacing is the gap between columns, ItemSpacing the gap between the items of a column
    [Fact]
    public void Calculate_MasonryWithDifferentSpacings_PutsLineSpacingBetweenColumnsAndItemSpacingBetweenItems()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(4), Masonry with { ItemSpacing = 6, LineSpacing = 20 }, new Size(220, 500));
        Assert.Equal([new Rect(0, 0, 100, 100), new Rect(120, 0, 100, 100), new Rect(0, 106, 100, 100),
            new Rect(120, 106, 100, 100)], layout.Bounds);
        Assert.Equal(new Size(220, 206), layout.Extent);
    }

    // The seven's tops are 0, 0, 0, 60, 110, 170, 210. Their middles, 50, 25, 100, 110, 160, 220, 260, are not in order
    [Theory]
    [InlineData(-50, 0)]
    [InlineData(30, 0)]   // as near the tops at 0 as the one at 60: the lowest index
    [InlineData(31, 3)]
    [InlineData(100, 4)]
    [InlineData(140, 4)]  // as near 110 as 170
    [InlineData(141, 5)]
    [InlineData(5000, 6)]
    public void IndexNearest_Masonry_PicksTheItemWhoseTopIsNearest(double position, int expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(SevenTiles(), Masonry, new Size(320, 500));
        Assert.Equal(expected, layout.IndexNearest(position));
    }

    [Fact]
    public void IndexNearest_DockedAfterMasonry_SearchesByTheMiddleAgain()
    {
        var layout = new GalleryLayout();
        layout.Calculate(SevenTiles(), Masonry, new Size(320, 500));
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal(5, layout.IndexNearest(550)); // every top in the strip is 0
    }

    [Theory]
    [InlineData(105, 50, 2, 4)] // 105 to 155: item 5 starts at 170, past it, and the search stops there
    [InlineData(165, 30, 2, 5)] // 165 to 195: item 3 ends at 160 and misses it, between items 2 and 4 that reach it
    public void IndicesIn_Masonry_ReturnsTheFirstAndLastItemsTheAreaReaches(double top, double height, int first, int last)
    {
        var layout = new GalleryLayout();
        layout.Calculate(SevenTiles(), Masonry, new Size(320, 500));
        Assert.Equal((first, last), layout.IndicesIn(new Rect(0, top, 320, height)));
    }

    [Theory]
    [InlineData(2, 50)]  // 200 tall from 0: its middle, 100, less half the view
    [InlineData(5, 170)] // 100 tall from 170, as tall as the view
    [InlineData(6, 210)] // clamped at the end: 310 − 100
    [InlineData(1, 0)]   // clamped at the start
    public void CenterOffset_MasonryInA100View_CentersOrClampsAlongTheHeight(int index, double expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(SevenTiles(), Masonry, new Size(320, 500));
        Assert.Equal(expected, layout.CenterOffset(index, 100));
    }

    [Fact]
    public void Calculate_MasonryCalculatedAgain_AllocatesNothing()
    {
        var items = Squares(500);
        var layout = new GalleryLayout();
        layout.Calculate(items, Masonry, new Size(1000, 500));

        var before = GC.GetAllocatedBytesForCurrentThread();
        layout.Calculate(items, Masonry, new Size(1000, 500));

        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static object?[] SevenInRows() =>
    [
        Item(300, 200), Item(300, 200), Item(300, 200), Item(100, 100), Item(200, 100), Item(100, 100),
        new GalleryItemViewModel()
    ];

    // At the target height, 150, a 3:2 item is 225 wide, a square 150 and a 2:1 item 300
    [Fact]
    public void Calculate_Rows_ScalesEachFullRowToEndAtTheWidthAndKeepsTheLastRowAtTheTargetHeight()
    {
        var layout = new GalleryLayout();
        layout.Calculate(SevenInRows(), Rows, new Size(430, 500));
        Assert.Equal(
        [
            // 225 + 10 + 225 is past 430, so both are (430 − 10) / 3 = 140 high
            new Rect(0, 0, 210, 140), new Rect(220, 0, 210, 140),
            // 225 + 10 + 150 fits and the 2:1 item goes past: (430 − 20) / 4.5 = 91.11 high, so 136.67 and 91.11 wide,
            // and the last item takes what is left of its 182.22
            new Rect(0, 150, 137, 91), new Rect(147, 150, 91, 91), new Rect(248, 150, 182, 91),
            // not filled: the target height, from the left. An item not read yet is a square
            new Rect(0, 251, 150, 150), new Rect(160, 251, 150, 150)
        ], layout.Bounds);
        Assert.Equal(new Size(430, 401), layout.Extent);
        Assert.True(layout.ScrollsVertically);
    }

    [Theory]
    [InlineData(310)]
    [InlineData(310.9)] // the part of a pixel is left over
    public void Calculate_RowsThatEndExactlyAtTheWidth_StayOpenUntilAnItemGoesPastIt(double width)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(2), Rows, new Size(width, 500)); // 150 + 10 + 150 is not wider than 310
        Assert.Equal([new Rect(0, 0, 150, 150), new Rect(160, 0, 150, 150)], layout.Bounds);
        Assert.Equal(new Size(width, 150), layout.Extent);

        layout.Calculate(Squares(3), Rows, new Size(width, 500)); // the third stays in the row: (310 − 20) / 3 = 96.67
        Assert.Equal([new Rect(0, 0, 97, 97), new Rect(107, 0, 97, 97), new Rect(214, 0, 96, 97)], layout.Bounds);
        Assert.Equal(new Size(width, 97), layout.Extent); // no row follows, so no spacing under it
    }

    [Fact]
    public void Calculate_RowsExtremeAspectRatios_ClampsThemToFourAndOneThird()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(4000, 100), Item(100, 2000)], Rows, new Size(430, 500));
        // 4:1 is 600 wide at 150: a row of its own, 430 / 4 = 107.5 high, which rounds to the even 108. 1:3 is 50 wide
        Assert.Equal([new Rect(0, 0, 430, 108), new Rect(0, 118, 50, 150)], layout.Bounds);
        Assert.Equal(new Size(430, 268), layout.Extent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(double.PositiveInfinity)]
    public void Calculate_RowsWithoutAUsableWidth_PlacesOneRowAtTheTargetHeight(double width)
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(200, 100), Item(100, 100)], Rows, new Size(width, 500));
        Assert.Equal([new Rect(0, 0, 300, 150), new Rect(310, 0, 150, 150)], layout.Bounds);
        Assert.Equal(new Size(460, 150), layout.Extent);
    }

    [Fact]
    public void Calculate_RowsWrapWidthOverride_WrapsAtItNotAtTheAvailableWidth()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(3), Rows with { WrapWidthOverride = 310 }, new Size(1000, 500));
        Assert.Equal<double>([0, 107, 214], layout.Bounds.Select(bounds => bounds.X).ToArray());
        Assert.Equal(new Size(310, 97), layout.Extent);
    }

    // At 125%, 344 is 430 device pixels, a spacing of 10 is 12.5, which rounds to 12, and the target height, 187.5,
    // rounds to the even 188. Two 3:2 items are 282 + 12 + 282 there: (430 − 12) / 3 = 139.33 high, 209 wide each
    [Fact]
    public void Calculate_RowsAtAFractionalScale_PutsWidthsHeightsAndGapsOnWholeDevicePixels()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(300, 200), Item(300, 200), Item(100, 100)], Rows with { LayoutScale = 1.25 },
            new Size(344, 500));
        Assert.Equal<double>([0, 221, 0], DevicePixels(layout.Bounds.Select(bounds => bounds.X), 1.25));
        Assert.Equal<double>([0, 0, 151], DevicePixels(layout.Bounds.Select(bounds => bounds.Y), 1.25)); // 139 + 12
        Assert.Equal<double>([209, 209, 188], DevicePixels(layout.Bounds.Select(bounds => bounds.Width), 1.25));
        Assert.Equal<double>([139, 139, 188], DevicePixels(layout.Bounds.Select(bounds => bounds.Height), 1.25));
        Assert.Equal(339, DevicePixels(layout.Extent.Height, 1.25)); // 151 + 188
        Assert.Equal(344, layout.Extent.Width);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    [InlineData(2.5)]
    public void Calculate_RowsVariedItemsAtAnyScale_EndsEveryFullRowAtTheWidthOnDevicePixels(double scale)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Varied(300), Rows with { LayoutScale = scale }, new Size(1000, 500));
        var bounds = layout.Bounds;

        AssertOnDevicePixels(bounds, scale);
        for (var i = 1; i < bounds.Count; i++)
        {
            Assert.True(bounds[i].Y >= bounds[i - 1].Y, $"Item {i} starts above item {i - 1}");
        }

        var rows = bounds.GroupBy(rect => rect.Y).Select(row => row.ToArray()).ToArray();
        Assert.Equal(57, rows.Length);
        foreach (var row in rows[..^1])
        {
            Assert.Equal(0, row[0].X);
            Assert.Equal(DevicePixels(1000, scale), DevicePixels(row[^1].Right, scale));
            Assert.All(row, rect => Assert.Equal(row[0].Height, rect.Height));
            Assert.True(DevicePixels(row[0].Height, scale) <= Math.Round(150 * scale));
            for (var i = 1; i < row.Length; i++)
            {
                Assert.Equal(Math.Round(10 * scale), DevicePixels(row[i].X - row[i - 1].Right, scale));
            }
        }

        var last = rows[^1]; // the last two items are both 1:3 after the clamp and do not fill a row
        Assert.Equal(2, last.Length);
        Assert.All(last, rect => Assert.Equal(Math.Round(150 * scale), DevicePixels(rect.Height, scale)));
        Assert.Equal(last[0].Bottom, layout.Extent.Height, 6);
    }

    // The design: ItemSpacing is the gap between the items of a row, LineSpacing the gap between rows
    [Fact]
    public void Calculate_RowsWithDifferentSpacings_PutsItemSpacingBetweenItemsAndLineSpacingBetweenRows()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(4), Rows with { ItemSpacing = 6, LineSpacing = 20 }, new Size(306, 500));
        // 150 + 6 + 150 is not wider than 306, and the third stays in the row: (306 - 12) / 3 = 98 high
        Assert.Equal([new Rect(0, 0, 98, 98), new Rect(104, 0, 98, 98), new Rect(208, 0, 98, 98),
            new Rect(0, 118, 150, 150)], layout.Bounds);
        Assert.Equal(new Size(306, 268), layout.Extent);
    }

    // The seven's tops in 430 are 0, 0, 150, 150, 150, 251, 251
    [Theory]
    [InlineData(75, 0)]   // as near 0 as 150: the lowest index
    [InlineData(76, 2)]   // the first item of the second row
    [InlineData(200, 2)]
    [InlineData(201, 5)]
    [InlineData(5000, 5)]
    public void IndexNearest_Rows_PicksTheFirstItemOfTheRowWhoseTopIsNearest(double position, int expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(SevenInRows(), Rows, new Size(430, 500));
        Assert.Equal(expected, layout.IndexNearest(position));
    }

    [Fact]
    public void IndicesIn_RowsAreaOverTheSecondRow_ReturnsThatRow()
    {
        var layout = new GalleryLayout();
        layout.Calculate(SevenInRows(), Rows, new Size(430, 500));
        Assert.Equal((2, 4), layout.IndicesIn(new Rect(0, 145, 430, 10))); // 145 to 155: the first row ends at 140
    }

    [Fact]
    public void Calculate_RowsCalculatedAgain_AllocatesNothing()
    {
        var items = Squares(500);
        var layout = new GalleryLayout();
        layout.Calculate(items, Rows, new Size(1000, 500));

        var before = GC.GetAllocatedBytesForCurrentThread();
        layout.Calculate(items, Rows, new Size(1000, 500));

        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
