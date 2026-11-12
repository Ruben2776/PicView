using PicView.Core.Svg.Parsing;

namespace PicView.Tests.Svg;

public class SvgPathDataTests
{
    [Theory]
    [InlineData("M0 0h10v10z", "M0,0 h10 v10 z")]
    [InlineData("M0 0a1 1 0 01.5.5", "M0,0 a1,1,0,0,1,.5,.5")]
    [InlineData("M10-5.5L.5.5 1e2,3", "M10,-5.5 L.5,.5 L100,3")]
    [InlineData("m1 2 3 4", "m1,2 l3,4")]
    public void TryNormalize_KeepsNumbersAndSeparatesThem(string d, string expected)
    {
        Assert.True(SvgPathData.TryNormalize(d, false, out var pathData));
        Assert.Equal(expected, pathData);
    }

    [Theory]
    [InlineData("+5", "5")]
    [InlineData("5.", "5")]
    [InlineData("-5.", "-5")]
    [InlineData("1.5e-3", "0.0015")]
    [InlineData("-2E+2", "-200")]
    [InlineData("1.50e1", "15.0")]
    [InlineData(".5e1", "5")]
    [InlineData("0012e-1", "1.2")]
    [InlineData("0e5", "0")]
    [InlineData("1e-500", "0")]
    public void TryNormalize_NumberFormAvaloniaRejects_IsRewrittenWithTheSameValue(string number, string written)
    {
        Assert.True(SvgPathData.TryNormalize("M" + number + " 0", false, out var pathData));
        Assert.Equal("M" + written + ",0", pathData);
    }

    [Fact]
    public void TryNormalize_TinyMantissaWithLargeExponent_KeepsTheValue()
    {
        // 1e-101 × 1e401 = 1e300.
        var number = "0." + new string('0', 100) + "1e401";
        Assert.True(SvgPathData.TryNormalize("M" + number + " 0", false, out var pathData));
        Assert.Equal("M1" + new string('0', 300) + ",0", pathData);
    }

    [Theory]
    [InlineData("1e-320")]
    [InlineData("123.456e-310")]
    [InlineData("-9.99e307")]
    [InlineData("0.00012e300")]
    [InlineData("1e-330")]
    [InlineData("1e-99999999999")]
    public void TryNormalize_ExponentForm_IsRewrittenWithTheSameDouble(string number)
    {
        Assert.True(SvgPathData.TryNormalize("M" + number + " 0", false, out var pathData));
        var written = pathData[1..pathData.IndexOf(',', StringComparison.Ordinal)];
        Assert.DoesNotContain("e", written, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(double.Parse(number, System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(written, System.Globalization.CultureInfo.InvariantCulture));
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
        // The S follows a Q and the T follows an S, so each starts from the current point.
        Assert.Equal("M0,0 C1,1,2,2,3,3 C4,4,5,5,6,6 Q1,1,2,2 C2,2,3,3,4,4 Q4,4,5,5 Z", pathData);
    }

    [Theory]
    [InlineData("M0 0 C1 1 2 2 3 3 S4 4 5 5 S6 6 7 7", "M0,0 C1,1,2,2,3,3 S4,4,5,5 S6,6,7,7")]
    [InlineData("M0 0 Q1 1 2 2 T3 3 T4 4", "M0,0 Q1,1,2,2 T3,3 T4,4")]
    [InlineData("M0 0 L1.5 0 S3 3 4 4 S5 5 6 6", "M0,0 L1.5,0 C1.5,0,3,3,4,4 S5,5,6,6")]
    [InlineData("M0 0 h1 s2 2 3 3 l1 1 t1 1 t2 2", "M0,0 h1 c0,0,2,2,3,3 l1,1 q0,0,1,1 t2,2")]
    [InlineData("M1 2 h3 v4 z T5 5", "M1,2 h3 v4 z Q1,2,5,5")]
    [InlineData("M0 0 a1 1 0 0 1 2 0 S3 3 4 4", "M0,0 a1,1,0,0,1,2,0 C2,0,3,3,4,4")]
    public void TryNormalize_SmoothCurve_IsExplicitUnlessItFollowsItsOwnKind(string d, string expected)
    {
        Assert.True(SvgPathData.TryNormalize(d, false, out var pathData));
        Assert.Equal(expected, pathData);
    }

    [Fact]
    public void TryNormalize_SmoothCurveAfterRelativeCommands_WritesTheExactCurrentPoint()
    {
        // 0.2 + 0.1 is 0.30000000000000004 in doubles, which is where Avalonia's own parser puts the point.
        Assert.True(SvgPathData.TryNormalize("m.1 .2 l.1 .1 T1 1", false, out var pathData));
        Assert.Equal("m.1,.2 l.1,.1 Q0.2,0.30000000000000004,1,1", pathData);
    }

    [Theory]
    [InlineData("M0 0 Z 1 1")]
    [InlineData("M0 0 a1 1 0 2 0 1 1")]
    [InlineData("M,0 0")]
    [InlineData("M0 0 L1e999 1")]
    [InlineData("M0 0 L1 1,")]
    // The S needs the current point, which is past the largest double.
    [InlineData("m1e308 0 l1e308 0 S1 1 2 2")]
    public void TryNormalize_Malformed_ReturnsFalse(string d)
    {
        Assert.False(SvgPathData.TryNormalize(d, false, out _));
    }
}
