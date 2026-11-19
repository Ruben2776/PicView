using PicView.Core.Exif;

namespace PicView.Tests.Exif;

public class ExifRationalParsingTests
{
    [Theory]
    [InlineData("1/0.5", 2d)]
    [InlineData("3/1.5", 2d)]
    [InlineData("0.5/2", 0.25d)]
    [InlineData("1/250", 0.004d)]
    [InlineData("28/10", 2.8d)]
    public void TryParseRational_Fraction_KeepsTheValue(string input, double expected)
    {
        var parsed = ExifFunctions.TryParseRational(input, out var result);

        Assert.True(parsed);
        Assert.NotEqual(0u, result.Denominator);
        Assert.Equal(expected, result.ToDouble(), 6);
    }

    [Theory]
    [InlineData("1/0")]
    [InlineData("-1/2")]
    [InlineData("1/-2")]
    public void TryParseRational_FractionWithoutAValidRatio_ReturnsFalse(string input)
    {
        Assert.False(ExifFunctions.TryParseRational(input, out _));
    }
}
