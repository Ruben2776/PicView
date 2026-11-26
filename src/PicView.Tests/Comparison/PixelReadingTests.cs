using PicView.Core.Comparison;

namespace PicView.Tests.Comparison;

/// <summary>
/// A Pixel Info reading: where a point of the shown image is in the image's own pixels, and how it is written.
/// </summary>
public class PixelReadingTests
{
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(99.9, 49.9, 0, 0)]
    [InlineData(100, 50, 1, 1)]
    [InlineData(399.9, 199.9, 3, 3)]
    [InlineData(250, 20, 2, 0)]
    public void TryLocate_OnTheImage_IsThePixelThePointIsIn(double x, double y, int pixelX, int pixelY)
    {
        // A 4 by 4 image shown 400 by 200
        Assert.True(PixelReading.TryLocate(x, y, 400, 200, 4, 4, out var foundX, out var foundY));

        Assert.Equal(pixelX, foundX);
        Assert.Equal(pixelY, foundY);
    }

    [Theory]
    [InlineData(-0.1, 10)]
    [InlineData(10, -0.1)]
    [InlineData(400, 10)]
    [InlineData(10, 200)]
    public void TryLocate_OffTheImage_IsFalse(double x, double y) =>
        Assert.False(PixelReading.TryLocate(x, y, 400, 200, 4, 4, out _, out _));

    [Theory]
    [InlineData(0, 4)]
    [InlineData(400, 0)]
    public void TryLocate_NothingShown_IsFalse(double shownWidth, int pixelWidth) =>
        Assert.False(PixelReading.TryLocate(0, 0, shownWidth, 200, pixelWidth, 4, out _, out _));

    [Fact]
    public void Text_OpaquePixel_IsThePlaceAndSixHexDigits() =>
        Assert.Equal("X: 1204 Y: 388  #3A7BD5", new PixelReading(1204, 388, 0xFF3A7BD5).Text);

    [Fact]
    public void Text_PixelThatIsNotOpaque_HasItsAlphaLast() =>
        Assert.Equal("X: 1 Y: 2  #3A7BD580", new PixelReading(1, 2, 0x803A7BD5).Text);

    [Fact]
    public void Text_BlackPixel_KeepsItsLeadingZeros() =>
        Assert.Equal("X: 0 Y: 0  #000000", new PixelReading(0, 0, 0xFF000000).Text);

    [Fact]
    public void Text_NoColor_IsThePlaceOnly() =>
        Assert.Equal("X: 0 Y: 0", default(PixelReading).Text);
}
