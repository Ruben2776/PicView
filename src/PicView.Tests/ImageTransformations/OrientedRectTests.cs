using PicView.Core.ImageTransformations;

namespace PicView.Tests.ImageTransformations;

public class OrientedRectTests
{
    private const uint SourceWidth = 40;
    private const uint SourceHeight = 30;

    [Fact]
    public void FromSource_OneTurnRight_MovesTheTopLeftCornerToTheTopRight()
    {
        var shown = OrientedRect.FromSource(new PixelArea(0, 0, 10, 5), new Orientation(1, false), SourceWidth,
            SourceHeight);

        Assert.Equal(new PixelArea(25, 0, 5, 10), shown);
    }

    [Fact]
    public void FromSource_HalfTurn_MovesItToTheOppositeCorner()
    {
        var shown = OrientedRect.FromSource(new PixelArea(0, 0, 10, 5), new Orientation(2, false), SourceWidth,
            SourceHeight);

        Assert.Equal(new PixelArea(30, 25, 10, 5), shown);
    }

    [Fact]
    public void FromSource_ThreeTurns_MovesTheTopLeftCornerToTheBottomLeft()
    {
        var shown = OrientedRect.FromSource(new PixelArea(0, 0, 10, 5), new Orientation(3, false), SourceWidth,
            SourceHeight);

        Assert.Equal(new PixelArea(0, 30, 5, 10), shown);
    }

    [Fact]
    public void FromSource_Flipped_MirrorsAcrossTheShownWidth()
    {
        var shown = OrientedRect.FromSource(new PixelArea(0, 0, 10, 5), new Orientation(0, true), SourceWidth,
            SourceHeight);

        Assert.Equal(new PixelArea(30, 0, 10, 5), shown);
    }

    [Fact]
    public void FromSource_OneTurnThenFlip_MirrorsTheTurnedRect()
    {
        var shown = OrientedRect.FromSource(new PixelArea(0, 0, 10, 5), new Orientation(1, true), SourceWidth,
            SourceHeight);

        Assert.Equal(new PixelArea(0, 0, 5, 10), shown);
    }

    [Fact]
    public void ShownSize_OddTurns_Swap()
    {
        Assert.Equal((30u, 40u), OrientedRect.ShownSize(new Orientation(1, false), SourceWidth, SourceHeight));
        Assert.Equal((40u, 30u), OrientedRect.ShownSize(new Orientation(2, true), SourceWidth, SourceHeight));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void ToSource_UndoesFromSource_ForAllEightOrientations(int quarterTurns, bool isFlipped)
    {
        var orientation = new Orientation(quarterTurns, isFlipped);
        var (shownWidth, shownHeight) = OrientedRect.ShownSize(orientation, SourceWidth, SourceHeight);
        PixelArea[] rects = [new(3, 4, 10, 5), new(0, 0, 40, 30), new(30, 25, 10, 5)];

        foreach (var rect in rects)
        {
            var shown = OrientedRect.FromSource(rect, orientation, SourceWidth, SourceHeight);

            Assert.True(shown.X >= 0 && shown.Y >= 0);
            Assert.True(shown.X + shown.Width <= shownWidth);
            Assert.True(shown.Y + shown.Height <= shownHeight);
            Assert.Equal(rect, OrientedRect.ToSource(shown, orientation, SourceWidth, SourceHeight));
        }
    }
}
