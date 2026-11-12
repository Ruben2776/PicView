using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class SvgParserShapeTests
{
    [Fact]
    public void Parse_Rect_WritesOutline()
    {
        Assert.Equal("F1 M10,20 H40 V60 H10 Z", Shape("<rect x='10' y='20' width='30' height='40'/>").PathData);
    }

    [Fact]
    public void Parse_Circle_WritesTwoArcs()
    {
        Assert.Equal("F1 M5,10 A5,5,0,1,0,15,10 A5,5,0,1,0,5,10 Z",
            Shape("<circle cx='10' cy='10' r='5'/>").PathData);
    }

    [Fact]
    public void Parse_Ellipse_WritesTwoArcs()
    {
        Assert.Equal("F1 M5,10 A5,3,0,1,0,15,10 A5,3,0,1,0,5,10 Z",
            Shape("<ellipse cx='10' cy='10' rx='5' ry='3'/>").PathData);
    }

    [Theory]
    [InlineData("<rect width='1' height='1' fill='none'/>")]
    [InlineData("<rect width='1' height='1' fill='none' stroke='red' stroke-width='0'/>")]
    [InlineData("<rect width='1' height='1' fill='url(#missing) none'/>")]
    [InlineData("<g opacity='.5'><rect width='1' height='1' fill='none'/></g>")]
    public void Parse_ShapeThatPaintsNothing_IsLeftOut(string body)
    {
        var root = Root(body);
        Assert.DoesNotContain(root.Children, node => node is SvgShape);
        Assert.All(root.Children, node => Assert.Empty(Assert.IsType<SvgGroup>(node).Children));
    }

    [Fact]
    public void Parse_ShapeThatPaintsNothing_StillCountsInAClipPath()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(Root(
            "<clipPath id='c'><rect width='5' height='5' fill='none'/></clipPath>" +
            "<g clip-path='url(#c)'><rect width='10' height='10'/></g>").Children));
        Assert.Single(group.Clip!.Shapes);
    }

    private static string TinyShapePath(string body)
    {
        var root = Drawing(Document(body, "width='100' height='100' viewBox='0 0 1 1'")).Root;
        return Assert.IsType<SvgShape>(Assert.Single(root.Children)).PathData;
    }

    [Fact]
    public void Parse_RectBelowAThousandth_KeepsEveryDigit()
    {
        Assert.Equal("F1 M0.2004,0.1 H0.2008 V0.6 H0.2004 Z",
            TinyShapePath("<rect x='0.2004' y='0.1' width='0.0004' height='0.5'/>"));
    }

    [Fact]
    public void Parse_CircleBelowAThousandth_KeepsItsRadius()
    {
        Assert.Equal("F1 M0.4996,0.5 A0.0004,0.0004,0,1,0,0.5004,0.5 A0.0004,0.0004,0,1,0,0.4996,0.5 Z",
            TinyShapePath("<circle cx='.5' cy='.5' r='.0004'/>"));
    }

    [Fact]
    public void Parse_Line_WritesMoveAndLine()
    {
        Assert.Equal("F1 M1,2 L3,4", Shape("<line x1='1' y1='2' x2='3' y2='4' stroke='red'/>").PathData);
    }

    [Fact]
    public void Parse_Polygon_ClosesPath()
    {
        Assert.Equal("F1 M0,0 L10,0 L10,10 Z", Shape("<polygon points='0,0 10,0 10,10'/>").PathData);
    }

    [Fact]
    public void Parse_Polyline_LeavesPathOpen()
    {
        Assert.Equal("F1 M0,0 L10,0 L10,10", Shape("<polyline points='0 0,10 0 , 10,10'/>").PathData);
    }

    [Fact]
    public void Parse_PathWithEvenOdd_HasNoNonZeroPrefix()
    {
        Assert.StartsWith("M", Shape("<path fill-rule='evenodd' d='M0 0h10v10z'/>").PathData);
    }

    [Fact]
    public void Parse_StrokeOnlyShapeWithEvenOdd_HasNoNonZeroPrefix()
    {
        Assert.Equal("M1,2 L3,4",
            Shape("<line x1='1' y1='2' x2='3' y2='4' stroke='red' fill-rule='evenodd'/>").PathData);
    }

    [Fact]
    public void Parse_Path_NormalizesData()
    {
        Assert.Equal("F1 M0,0 h10 v10 z", Shape("<path d='M0 0h10v10z'/>").PathData);
    }

    [Fact]
    public void Parse_ZeroSizeRect_IsSkipped()
    {
        Assert.Empty(Root("<rect width='0' height='10'/>").Children);
    }

    [Theory]
    [InlineData("<rect width='10'/>")]
    [InlineData("<circle r='0'/>")]
    [InlineData("<ellipse rx='5' ry='0'/>")]
    [InlineData("<path d=''/>")]
    [InlineData("<path/>")]
    [InlineData("<polygon points='1,1'/>")]
    [InlineData("<polyline points=''/>")]
    public void Parse_EmptyShape_IsSkipped(string body)
    {
        Assert.Empty(Root(body).Children);
    }

    [Fact]
    public void Parse_RoundedRect_WritesFourArcCorners()
    {
        Assert.Equal(
            "F1 M2,0 H8 A2,3,0,0,1,10,3 V17 A2,3,0,0,1,8,20 H2 A2,3,0,0,1,0,17 V3 A2,3,0,0,1,2,0 Z",
            Shape("<rect width='10' height='20' rx='2' ry='3'/>").PathData);
    }

    [Fact]
    public void Parse_RoundedRectWithOnlyRx_UsesRxForRy()
    {
        Assert.Equal(
            "F1 M2,0 H8 A2,2,0,0,1,10,2 V18 A2,2,0,0,1,8,20 H2 A2,2,0,0,1,0,18 V2 A2,2,0,0,1,2,0 Z",
            Shape("<rect width='10' height='20' rx='2'/>").PathData);
    }

    [Fact]
    public void Parse_RoundedRectWithOnlyRy_UsesRyForRx()
    {
        Assert.Equal(
            "F1 M2,0 H8 A2,2,0,0,1,10,2 V18 A2,2,0,0,1,8,20 H2 A2,2,0,0,1,0,18 V2 A2,2,0,0,1,2,0 Z",
            Shape("<rect width='10' height='20' ry='2'/>").PathData);
    }

    [Fact]
    public void Parse_RoundedRectRadiusTooLarge_IsClampedToHalfTheSide()
    {
        Assert.Equal(
            "F1 M5,0 H5 A5,10,0,0,1,10,10 V10 A5,10,0,0,1,5,20 H5 A5,10,0,0,1,0,10 V10 A5,10,0,0,1,5,0 Z",
            Shape("<rect width='10' height='20' rx='50'/>").PathData);
    }

    [Fact]
    public void Parse_RoundedRectWithZeroRadius_IsPlainRect()
    {
        Assert.Equal("F1 M0,0 H10 V20 H0 Z", Shape("<rect width='10' height='20' rx='0' ry='4'/>").PathData);
    }

    [Theory]
    [InlineData("<rect width='1' height='-1'/>", "value:height")]
    [InlineData("<rect width='1' height='1' rx='-1'/>", "value:rx")]
    [InlineData("<circle r='-1'/>", "value:r")]
    [InlineData("<ellipse rx='1' ry='-1'/>", "value:ry")]
    [InlineData("<rect width='abc' height='1'/>", "value:width")]
    [InlineData("<polygon points='0,0 1'/>", "value:points")]
    [InlineData("<polygon points='0,0 1,a'/>", "value:points")]
    [InlineData("<rect x='1e308' width='1e308' height='1'/>", "value:width")]
    [InlineData("<rect width='1' y='1e308' height='1e308'/>", "value:height")]
    [InlineData("<circle cx='-1e308' r='1e308'/>", "value:r")]
    [InlineData("<ellipse cx='1e308' rx='1e308' ry='1'/>", "value:rx")]
    public void Parse_InvalidShapeValue_GivesReason(string body, string reason)
    {
        Assert.Equal(reason, Reason(Document(body)));
    }

    [Fact]
    public void Parse_ShapeTransform_WrapsShapeInGroup()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root("<rect width='1' height='1' transform='translate(10,0) scale(2)'/>").Children));
        Assert.NotNull(group.Transform);
        Assert.Equal((12d, 0d), group.Transform.Value.Apply(1, 0));
        Assert.Equal(1, group.Opacity);
        Assert.IsType<SvgShape>(Assert.Single(group.Children));
    }

    [Fact]
    public void Parse_ShapeOpacity_WrapsShapeInGroup()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root("<rect width='1' height='1' opacity='0.25'/>").Children));
        Assert.Null(group.Transform);
        Assert.Equal(0.25, group.Opacity);
        Assert.IsType<SvgShape>(Assert.Single(group.Children));
    }

    [Fact]
    public void Parse_PlainGroup_IsKeptAsGroup()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(Root("<g><rect width='1' height='1'/></g>").Children));
        Assert.Null(group.Transform);
        Assert.Equal(1, group.Opacity);
        Assert.Null(group.Clip);
        Assert.IsType<SvgShape>(Assert.Single(group.Children));
    }

    [Theory]
    [InlineData("matrix(1 2 3 4 5 6)", 1, 1, 1 + 3 + 5d, 2 + 4 + 6d)]
    [InlineData("translate(5)", 1, 1, 6d, 1d)]
    [InlineData("scale(2,3)", 1, 1, 2d, 3d)]
    [InlineData("scale(2)", 1, 1, 2d, 2d)]
    [InlineData("rotate(90)", 1, 0, 0d, 1d)]
    [InlineData("rotate(90 10 10)", 10, 0, 20d, 10d)]
    [InlineData("skewX(45)", 0, 1, 1d, 1d)]
    [InlineData("skewY(45)", 1, 0, 1d, 1d)]
    [InlineData("translate(1,0),scale(2)", 1, 0, 3d, 0d)]
    public void Parse_GroupTransform_AppliesSvgTransformList(string transform, double x, double y, double ex,
        double ey)
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root($"<g transform='{transform}'><rect width='1' height='1'/></g>").Children));
        Assert.NotNull(group.Transform);
        var (ax, ay) = group.Transform.Value.Apply(x, y);
        Assert.Equal(ex, ax, 9);
        Assert.Equal(ey, ay, 9);
    }

    [Theory]
    [InlineData("translate(1")]
    [InlineData("spin(4)")]
    [InlineData("scale()")]
    [InlineData("matrix(1 2 3)")]
    public void Parse_InvalidTransform_GivesReason(string transform)
    {
        Assert.Equal("value:transform",
            Reason(Document($"<rect width='1' height='1' transform='{transform}'/>")));
    }
}
