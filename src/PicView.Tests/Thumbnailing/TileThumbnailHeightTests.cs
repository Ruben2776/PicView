using PicView.Core.Thumbnailing;

namespace PicView.Tests.Thumbnailing;

public class TileThumbnailHeightTests
{
    [Theory]
    [InlineData(300, 200, 6000, 4000, 200u)] // same shape: the tile's height
    [InlineData(300, 900, 1000, 10000, 3000u)] // very tall image in a tile clamped to 1:3: cover the width
    [InlineData(1200, 300, 10000, 1000, 300u)] // very wide image in a tile clamped to 4:1: the tile's height covers
    [InlineData(300, 200, 150, 100, 100u)] // never more than the image's own height
    [InlineData(300, 300, 0, 0, 300u)] // unreadable: a square, no cap
    [InlineData(300.4, 200.2, 6000, 4000, 201u)] // rounded up
    [InlineData(0, 0, 6000, 4000, 1u)] // never zero
    [InlineData(double.NaN, double.NaN, 6000, 4000, 1u)] // not a size: never uint.MaxValue
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, 6000, 4000, 1u)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, 0, 0, 1u)] // nor uncapped
    [InlineData(double.NaN, 200, 6000, 4000, 200u)] // the other side still counts
    public void For_GivesTheSmallestHeightThatCoversTheTile(double tileWidth, double tileHeight,
        uint pixelWidth, uint pixelHeight, uint expected) =>
        Assert.Equal(expected, TileThumbnailHeight.For(tileWidth, tileHeight, pixelWidth, pixelHeight));
}
