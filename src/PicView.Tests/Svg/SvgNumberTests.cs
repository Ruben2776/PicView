using System.Globalization;
using PicView.Core.Svg;

namespace PicView.Tests.Svg;

public class SvgNumberTests
{
    [Theory]
    [InlineData(1.5, "1.5")]
    [InlineData(2.0, "2")]
    [InlineData(0.12345, "0.12345")]
    [InlineData(-0.0001, "-0.0001")]
    [InlineData(-0.0, "0")]
    [InlineData(0.015625, "0.015625")]
    [InlineData(0.0004, "0.0004")]
    [InlineData(1e-7, "0.0000001")]
    [InlineData(-2.5e-10, "-0.00000000025")]
    [InlineData(1e21, "1000000000000000000000")]
    [InlineData(1.0 / 3, "0.3333333333333333")]
    [InlineData(0.1 + 0.2, "0.30000000000000004")]
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
    [InlineData(5e-324)]
    [InlineData(double.MaxValue)]
    [InlineData(-1.2345678901234567e-300)]
    [InlineData(0.1 + 0.2)]
    [InlineData(123456.789e10)]
    public void Format_AnyFiniteValue_RoundTripsWithoutAnExponent(double value)
    {
        var text = SvgNumber.Format(value);
        Assert.DoesNotContain("E", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(value, double.Parse(text, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(0.1, "10")]
    [InlineData(0.3, "30")]
    [InlineData(0.07, "7")]
    [InlineData(1.0 / 3, "33.33333333333333")]
    [InlineData(0, "0")]
    [InlineData(1, "100")]
    [InlineData(-0.5, "-50")]
    [InlineData(0.005, "0.5")]
    [InlineData(0.00125, "0.125")]
    [InlineData(12.5, "1250")]
    public void FormatPercent_MovesTheDecimalPointWithoutMultiplying(double value, string expected)
    {
        Assert.Equal(expected, SvgNumber.FormatPercent(value));
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
