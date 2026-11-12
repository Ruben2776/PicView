using PicView.Core.Svg;
using PicView.Core.Svg.Parsing;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class SvgParserPaintTests
{
    [Fact]
    public void Parse_NamedFill_IsSolidPaint()
    {
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 255, 0, 0)),
            Shape("<rect width='1' height='1' fill='red'/>").Fill);
    }

    [Fact]
    public void Parse_FillOpacity_IsFoldedIntoAlpha()
    {
        Assert.Equal(new SvgColor(128, 0, 0, 255), Solid("<rect width='1' height='1' fill='#00f' fill-opacity='0.5'/>"));
    }

    [Fact]
    public void Parse_NoFill_IsBlack()
    {
        Assert.Equal(SvgColor.Black, Solid("<rect width='1' height='1'/>"));
    }

    [Fact]
    public void Parse_FillNone_HasNoFill()
    {
        Assert.Null(Shape("<rect width='1' height='1' fill='none'/>").Fill);
    }

    [Fact]
    public void Parse_NoStroke_HasNoStroke()
    {
        Assert.Null(Shape("<rect width='1' height='1'/>").Stroke);
    }

    [Fact]
    public void Parse_Stroke_HasDefaults()
    {
        var stroke = Shape("<rect width='1' height='1' stroke='red'/>").Stroke;
        Assert.NotNull(stroke);
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 255, 0, 0)), stroke.Paint);
        Assert.Equal(1, stroke.Width);
        Assert.Equal(SvgLineCap.Butt, stroke.LineCap);
        Assert.Equal(SvgLineJoin.Miter, stroke.LineJoin);
        Assert.Equal(4, stroke.MiterLimit);
        Assert.Null(stroke.DashArray);
        Assert.Equal(0, stroke.DashOffset);
    }

    [Fact]
    public void Parse_StrokeProperties_AreRead()
    {
        var stroke = Shape("<rect width='1' height='1' stroke='#0000ff' stroke-opacity='0.5' stroke-width='3' " +
                           "stroke-linecap='round' stroke-linejoin='bevel' stroke-miterlimit='8' " +
                           "stroke-dasharray='1 2' stroke-dashoffset='-2'/>").Stroke;
        Assert.NotNull(stroke);
        Assert.Equal(new SvgSolidPaint(new SvgColor(128, 0, 0, 255)), stroke.Paint);
        Assert.Equal(3, stroke.Width);
        Assert.Equal(SvgLineCap.Round, stroke.LineCap);
        Assert.Equal(SvgLineJoin.Bevel, stroke.LineJoin);
        Assert.Equal(8, stroke.MiterLimit);
        Assert.Equal([1d, 2d], stroke.DashArray);
        Assert.Equal(-2, stroke.DashOffset);
    }

    [Fact]
    public void Parse_StrokeWidthZero_HasNoStroke()
    {
        Assert.Null(Shape("<rect width='1' height='1' stroke='red' stroke-width='0'/>").Stroke);
    }

    [Fact]
    public void Parse_OddDashArray_IsRepeated()
    {
        var stroke = Shape("<rect width='1' height='1' stroke='red' stroke-dasharray='1,2,3'/>").Stroke;
        Assert.NotNull(stroke);
        Assert.Equal([1d, 2d, 3d, 1d, 2d, 3d], stroke.DashArray);
    }

    [Theory]
    [InlineData("0 0")]
    [InlineData("none")]
    public void Parse_DashArrayWithoutDashes_IsNull(string dashArray)
    {
        var stroke = Shape($"<rect width='1' height='1' stroke='red' stroke-dasharray='{dashArray}'/>").Stroke;
        Assert.NotNull(stroke);
        Assert.Null(stroke.DashArray);
    }

    [Fact]
    public void Parse_StyleAttribute_BeatsPresentationAttribute()
    {
        Assert.Equal(new SvgColor(255, 0, 0, 255), Solid("<rect width='1' height='1' fill='red' style='fill:blue'/>"));
    }

    [Fact]
    public void Parse_StylePropertyNamesAndKeywords_AreCaseInsensitiveAndTrimmed()
    {
        var shape = Shape("<rect width='1' height='1' style=' FILL : Blue ; Stroke:RED;stroke-LINECAP: Square '/>");
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 0, 0, 255)), shape.Fill);
        Assert.NotNull(shape.Stroke);
        Assert.Equal(SvgLineCap.Square, shape.Stroke.LineCap);
    }

    [Fact]
    public void Parse_FillOnGroup_IsInherited()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root("<g fill='green'><rect width='1' height='1'/></g>").Children));
        var shape = Assert.IsType<SvgShape>(Assert.Single(group.Children));
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 0, 128, 0)), shape.Fill);
    }

    [Fact]
    public void Parse_OpacityOnGroup_IsNotInherited()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root("<g opacity='0.5'><rect width='1' height='1'/></g>").Children));
        Assert.Equal(0.5, group.Opacity);
        var shape = Assert.IsType<SvgShape>(Assert.Single(group.Children));
        Assert.Equal(new SvgSolidPaint(SvgColor.Black), shape.Fill);
    }

    [Fact]
    public void Parse_CurrentColor_UsesOptionsColor()
    {
        var options = new SvgParseOptions { CurrentColor = new SvgColor(255, 1, 2, 3) };
        Assert.Equal(new SvgColor(255, 1, 2, 3), Solid("<rect width='1' height='1' fill='currentColor'/>", options));
    }

    [Fact]
    public void Parse_ColorOnGroup_OverridesCurrentColorForSubtree()
    {
        var options = new SvgParseOptions { CurrentColor = new SvgColor(255, 1, 2, 3) };
        var root = Root("<g color='blue'><rect width='1' height='1' fill='currentColor'/></g>" +
                        "<rect width='1' height='1' fill='currentColor'/>", options);
        var inside = Assert.IsType<SvgShape>(Assert.Single(Assert.IsType<SvgGroup>(root.Children[0]).Children));
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 0, 0, 255)), inside.Fill);
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 1, 2, 3)), Assert.IsType<SvgShape>(root.Children[1]).Fill);
    }

    [Theory]
    [InlineData("rgb(255,0,0)", 255, 255, 0, 0)]
    [InlineData("rgb( 100% , 0% , 50% )", 255, 255, 0, 128)]
    [InlineData("rgba(0,0,255,0.5)", 128, 0, 0, 255)]
    [InlineData("rgb(0 0 255 / 50%)", 128, 0, 0, 255)]
    [InlineData("#11223344", 0x44, 0x11, 0x22, 0x33)]
    [InlineData("#1234", 0x44, 0x11, 0x22, 0x33)]
    [InlineData("#ABCDEF", 255, 0xAB, 0xCD, 0xEF)]
    [InlineData("transparent", 0, 0, 0, 0)]
    [InlineData("cornflowerblue", 255, 0x64, 0x95, 0xED)]
    [InlineData("CornFlowerBlue", 255, 0x64, 0x95, 0xED)]
    public void Parse_ColorSyntax_GivesColor(string fill, byte a, byte r, byte g, byte b)
    {
        Assert.Equal(new SvgColor(a, r, g, b), Solid($"<rect width='1' height='1' fill='{fill}'/>"));
    }

    [Fact]
    public void NamedColors_HasTheFullCssList()
    {
        Assert.Equal(147, SvgColorParser.NamedColorCount);
    }

    [Theory]
    [InlineData("<rect width='1' height='1' display='none'/>")]
    [InlineData("<rect width='1' height='1' style='display:none'/>")]
    [InlineData("<rect width='1' height='1' visibility='hidden'/>")]
    [InlineData("<rect width='1' height='1' visibility='collapse'/>")]
    public void Parse_HiddenShape_IsAbsent(string body)
    {
        Assert.Empty(Root(body).Children);
    }

    [Fact]
    public void Parse_DisplayNoneOnGroup_RemovesSubtree()
    {
        Assert.Empty(Root("<g display='none'><rect width='1' height='1' display='inline'/></g>").Children);
    }

    [Fact]
    public void Parse_VisibleChildOfHiddenGroup_IsDrawn()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(Root(
            "<g visibility='hidden'><rect width='1' height='1'/><rect width='2' height='2' visibility='visible'/></g>")
            .Children));
        var shape = Assert.IsType<SvgShape>(Assert.Single(group.Children));
        Assert.Equal("F1 M0,0 H2 V2 H0 Z", shape.PathData);
    }

    [Theory]
    [InlineData("1in", "F1 M0,0 H96 V1 H0 Z")]
    [InlineData("10mm", "F1 M0,0 H37.795 V1 H0 Z")]
    [InlineData("2em", "F1 M0,0 H32 V1 H0 Z")]
    [InlineData("12pt", "F1 M0,0 H16 V1 H0 Z")]
    [InlineData("1pc", "F1 M0,0 H16 V1 H0 Z")]
    [InlineData("2.54cm", "F1 M0,0 H96 V1 H0 Z")]
    [InlineData("1rem", "F1 M0,0 H16 V1 H0 Z")]
    [InlineData("7px", "F1 M0,0 H7 V1 H0 Z")]
    public void Parse_LengthUnits_ConvertToPixels(string width, string expected)
    {
        Assert.Equal(expected, Shape($"<rect width='{width}' height='1'/>").PathData);
    }

    [Fact]
    public void Parse_PercentWidth_UsesViewportWidth()
    {
        var root = Drawing(Document("<rect width='50%' height='10%'/>", "width='200' height='100'")).Root;
        Assert.Equal("F1 M0,0 H100 V10 H0 Z", Assert.IsType<SvgShape>(Assert.Single(root.Children)).PathData);
    }

    [Fact]
    public void Parse_PercentWithViewBox_UsesViewBoxSize()
    {
        var root = Drawing(Document("<rect width='50%' height='50%'/>",
            "width='400' height='400' viewBox='0 0 20 40'")).Root;
        Assert.Equal("F1 M0,0 H10 V20 H0 Z", Assert.IsType<SvgShape>(Assert.Single(root.Children)).PathData);
    }

    [Fact]
    public void Parse_PercentRadius_UsesNormalizedDiagonal()
    {
        // sqrt((30² + 40²) / 2) = 35.355, 10% of it is 3.536
        var root = Drawing(Document("<circle r='10%'/>", "width='30' height='40'")).Root;
        Assert.Equal("F1 M-3.536,0 A3.536,3.536,0,1,0,3.536,0 A3.536,3.536,0,1,0,-3.536,0 Z",
            Assert.IsType<SvgShape>(Assert.Single(root.Children)).PathData);
    }

    [Theory]
    [InlineData("stroke-linecap='wide'", "value:stroke-linecap")]
    [InlineData("stroke-linejoin='miter-clip'", "value:stroke-linejoin")]
    [InlineData("stroke-linejoin='arcs'", "value:stroke-linejoin")]
    [InlineData("fill-rule='odd'", "value:fill-rule")]
    [InlineData("display='sometimes'", "value:display")]
    [InlineData("visibility='maybe'", "value:visibility")]
    [InlineData("stroke-dasharray='1 -2'", "value:stroke-dasharray")]
    [InlineData("stroke='url(#g)'", "value:stroke")]
    [InlineData("fill='url(#g)'", "value:fill")]
    [InlineData("opacity='half'", "value:opacity")]
    [InlineData("stroke-width='-1'", "value:stroke-width")]
    [InlineData("color='notacolor'", "value:color")]
    [InlineData("fill='rgb(10%, 20, 30)'", "value:fill")]
    [InlineData("stroke='rgba(10, 20%, 30, 0.5)'", "value:stroke")]
    public void Parse_UnknownKeywordValue_GivesReason(string attribute, string reason)
    {
        Assert.Equal(reason, Reason(Document($"<rect width='1' height='1' {attribute}/>")));
    }
}
