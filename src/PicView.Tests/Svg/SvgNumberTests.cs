using System.Globalization;
using PicView.Core.Svg;

namespace PicView.Tests.Svg;

public class SvgNumberTests
{
    [Theory]
    [InlineData(1.5, "1.5")]
    [InlineData(2.0, "2")]
    [InlineData(0.12345, "0.123")]
    [InlineData(-0.0001, "0")]
    public void Format_UnderDanishCulture_IsInvariant(double value, string expected)
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");
            Assert.Equal(expected, SvgNumber.Format(value));
        }
        finally
        {
            CultureInfo.CurrentCulture = old;
        }
    }

    [Theory]
    [InlineData("1e999")]
    [InlineData("NaN")]
    [InlineData("1,5")]
    [InlineData("")]
    [InlineData("Infinity")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("1,000.5")]
    public void TryParse_NotAFiniteNumber_ReturnsFalse(string text)
    {
        Assert.False(SvgNumber.TryParse(text, out _));
    }

    [Theory]
    [InlineData(".5", 0.5)]
    [InlineData("-1", -1)]
    [InlineData("1e2", 100)]
    [InlineData("+3", 3)]
    [InlineData("2.25", 2.25)]
    public void TryParse_ValidNumber_ReturnsValue(string text, double expected)
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");
            Assert.True(SvgNumber.TryParse(text, out var value));
            Assert.Equal(expected, value);
        }
        finally
        {
            CultureInfo.CurrentCulture = old;
        }
    }
}
