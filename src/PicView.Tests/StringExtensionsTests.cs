using System.Globalization;
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
    [InlineData(300.0, 300.0, "dpi")]
    [InlineData(72.5, 96.0, "DPI")]
    [InlineData(0.0, 0.0, "dpi")]
    [InlineData(-1.7976931348623157E+308, double.MaxValue, "dpi")]
    public void CombineSizeWithSuffix_ValidInputs_MatchesInterpolation(double x, double y, string suffix)
    {
        var expected = $"{x} x {y} {suffix}";
        var result = StringExtensions.CombineSizeWithSuffix(x, y, suffix);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CombineSizeWithSuffix_NoSuffix_LeavesOutTrailingSpace(string? suffix)
    {
        var expected = $"{300.0} x {150.0}";
        var result = StringExtensions.CombineSizeWithSuffix(300.0, 150.0, suffix);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CombineSizeWithSuffix_AllocatesOnlyTheResult()
    {
        StringExtensions.CombineSizeWithSuffix(300.0, 300.0, "dpi"); // warm up

        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = StringExtensions.CombineSizeWithSuffix(300.0, 300.0, "dpi");
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // A string object is a 20-byte header (on 64-bit) plus 2 bytes per char, rounded up to 8 bytes
        var stringSize = (20 + result.Length * 2 + 7) & ~7;
        Assert.Equal(stringSize, allocated);
    }

    [Theory]
    [InlineData(100.0)]
    [InlineData(50.5)]
    public void CombineWithPercentage_ValidInputs_ReturnsExpectedString(double zoom)
    {
        var expected = $"{zoom.ToString(CultureInfo.CurrentCulture)}%";
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
