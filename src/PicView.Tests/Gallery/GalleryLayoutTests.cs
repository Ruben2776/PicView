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
        item.SetDetails(new GalleryFileDetails(width, height, null));
        return item;
    }

    private static object?[] Squares(int count) => Enumerable.Range(0, count).Select(_ => (object?)Item(100, 100)).ToArray();

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
            ((GalleryItemViewModel)item!).SetDetails(new GalleryFileDetails(200, 100, null));
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
            ((GalleryItemViewModel)item!).SetDetails(new GalleryFileDetails(200, 100, null));
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
}
