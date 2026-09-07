using PicView.Core.Extensions;

namespace PicView.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData(1, 100, "1/100")]
    [InlineData(50, 100, "50/100")]
    [InlineData(0, 0, "0/0")]
    [InlineData(1234, 5678, "1234/5678")]
    [InlineData(-5, 10, "-5/10")]
    [InlineData(1, 1, "1/1")]
    public void CombineProgress_ValidInputs_ReturnsExpectedString(int current, int total, string expected)
    {
        var result = StringExtensions.CombineProgress(current, total);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1920u, 1080u, "1920 x 1080")]
    [InlineData(0u, 0u, "0 x 0")]
    public void CombineSize_ValidInputs_ReturnsExpectedString(uint width, uint height, string expected)
    {
        var result = StringExtensions.CombineSize(width, height);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(100.0, "100%")]
    [InlineData(50.5, "50.5%")]
    public void CombineWithPercentage_ValidInputs_ReturnsExpectedString(double zoom, string expected)
    {
        var result = StringExtensions.CombineWithPercentage(zoom);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Left", "Right", "Left || Right")]
    [InlineData("", "Right", " || Right")]
    public void CombineWithSeparator_ValidInputs_ReturnsExpectedString(string a, string b, string expected)
    {
        var result = StringExtensions.CombineWithSeparator(a, b);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Image.png", "Image.png - PicView")]
    [InlineData("", "PicView")]
    [InlineData(null, "PicView")]
    public void CombineWithAppName_ValidInputs_ReturnsExpectedString(string? value, string expected)
    {
        var result = StringExtensions.CombineWithAppName(value);
        Assert.Equal(expected, result);
    }
}
