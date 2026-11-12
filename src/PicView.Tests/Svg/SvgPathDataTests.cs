using PicView.Core.Svg.Parsing;

namespace PicView.Tests.Svg;

public class SvgPathDataTests
{
    [Theory]
    [InlineData("M0 0h10v10z", "M0,0 h10 v10 z")]
    [InlineData("M0 0a1 1 0 01.5.5", "M0,0 a1,1,0,0,1,.5,.5")]
    [InlineData("M10-5.5L.5.5 1e2,3", "M10,-5.5 L.5,.5 L1e2,3")]
    [InlineData("m1 2 3 4", "m1,2 l3,4")]
    public void TryNormalize_KeepsNumbersAndSeparatesThem(string d, string expected)
    {
        Assert.True(SvgPathData.TryNormalize(d, false, out var pathData));
        Assert.Equal(expected, pathData);
    }

    [Theory]
    [InlineData("M0 0 X 5")]
    [InlineData("M 1")]
    [InlineData("L1 1")]
    public void TryNormalize_Invalid_ReturnsFalse(string d)
    {
        Assert.False(SvgPathData.TryNormalize(d, false, out _));
    }

    [Fact]
    public void TryNormalize_NonZero_AddsPrefix()
    {
        Assert.True(SvgPathData.TryNormalize("M0 0 L1 1", true, out var pathData));
        Assert.Equal("F1 M0,0 L1,1", pathData);
    }

    [Fact]
    public void TryNormalize_CurvesAndRepeats_GiveEachRepeatItsLetter()
    {
        Assert.True(SvgPathData.TryNormalize("M0,0 C1,1 2,2 3,3 4,4 5,5 6,6 Q1 1 2 2 S3 3 4 4 T5 5 Z", false,
            out var pathData));
        Assert.Equal("M0,0 C1,1,2,2,3,3 C4,4,5,5,6,6 Q1,1,2,2 S3,3,4,4 T5,5 Z", pathData);
    }

    [Theory]
    [InlineData("M0 0 Z 1 1")]
    [InlineData("M0 0 a1 1 0 2 0 1 1")]
    [InlineData("M,0 0")]
    [InlineData("M0 0 L1e999 1")]
    [InlineData("M0 0 L1 1,")]
    public void TryNormalize_Malformed_ReturnsFalse(string d)
    {
        Assert.False(SvgPathData.TryNormalize(d, false, out _));
    }
}
