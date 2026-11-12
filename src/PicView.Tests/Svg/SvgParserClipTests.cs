using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class SvgParserClipTests
{
    private static SvgGroup ClippedGroup(string body)
    {
        return Assert.IsType<SvgGroup>(Assert.Single(Root(body).Children));
    }

    [Fact]
    public void Parse_ClipPath_BecomesClipShapesWithClipRule()
    {
        var group = ClippedGroup(
            "<defs><clipPath id='c'><circle cx='5' cy='5' r='5'/><rect width='2' height='2' clip-rule='evenodd'/>" +
            "</clipPath></defs><g clip-path='url(#c)'><rect width='10' height='10'/></g>");
        Assert.NotNull(group.Clip);
        Assert.Equal(2, group.Clip.Shapes.Count);
        Assert.StartsWith("F1 ", group.Clip.Shapes[0].PathData, StringComparison.Ordinal);
        Assert.False(group.Clip.Shapes[1].PathData.StartsWith("F1 ", StringComparison.Ordinal));
        Assert.Null(group.Clip.Shapes[0].Transform);
        Assert.IsType<SvgShape>(Assert.Single(group.Children));
    }

    [Fact]
    public void Parse_ClipRuleOnClipPath_IsInherited()
    {
        var group = ClippedGroup(
            "<clipPath id='c' clip-rule='evenodd'><rect width='2' height='2'/></clipPath>" +
            "<g clip-path='url(#c)'><rect width='10' height='10'/></g>");
        Assert.NotNull(group.Clip);
        Assert.Equal("M0,0 H2 V2 H0 Z", Assert.Single(group.Clip.Shapes).PathData);
    }

    [Fact]
    public void Parse_ObjectBoundingBoxClip_IsProperty()
    {
        Assert.Equal("property:clipPathUnits", Reason(Document(
            "<clipPath id='c' clipPathUnits='objectBoundingBox'><rect width='1' height='1'/></clipPath>" +
            "<rect width='1' height='1' clip-path='url(#c)'/>")));
    }

    [Fact]
    public void Parse_UserSpaceClipUnits_IsAccepted()
    {
        var group = ClippedGroup(
            "<clipPath id='c' clipPathUnits='userSpaceOnUse'><rect width='1' height='1'/></clipPath>" +
            "<rect width='1' height='1' clip-path='url(#c)'/>");
        Assert.NotNull(group.Clip);
    }

    [Theory]
    [InlineData("<clipPath id='c'><text/></clipPath>")]
    [InlineData("<clipPath id='c'><g><rect width='1' height='1'/></g></clipPath><rect width='1' height='1' clip-path='url(#c)'/>")]
    [InlineData("<g id='g'><rect width='1' height='1'/></g><clipPath id='c'><use href='#g'/></clipPath><rect width='1' height='1' clip-path='url(#c)'/>")]
    [InlineData("<clipPath id='d'><rect width='1' height='1'/></clipPath><clipPath id='c' clip-path='url(#d)'><rect width='1' height='1'/></clipPath><rect width='1' height='1' clip-path='url(#c)'/>")]
    [InlineData("<clipPath id='d'><rect width='1' height='1'/></clipPath><clipPath id='c'><rect width='1' height='1' clip-path='url(#d)'/></clipPath><rect width='1' height='1' clip-path='url(#c)'/>")]
    public void Parse_UnsupportedClipPathContent_IsElementClipPath(string body)
    {
        Assert.Equal("element:clipPath", Reason(Document(body)));
    }

    [Fact]
    public void Parse_ClipShapeTransform_IsChildThenClipPath()
    {
        var group = ClippedGroup(
            "<clipPath id='c' transform='translate(10,0)'><rect width='1' height='1' transform='scale(2)'/></clipPath>" +
            "<rect width='1' height='1' clip-path='url(#c)'/>");
        Assert.NotNull(group.Clip);
        var transform = Assert.Single(group.Clip.Shapes).Transform;
        Assert.NotNull(transform);
        Assert.Equal((12d, 0d), transform.Value.Apply(1, 0));
    }

    [Fact]
    public void Parse_UseOfShapeInClipPath_IsClipShape()
    {
        var group = ClippedGroup(
            "<defs><rect id='r' width='1' height='1' transform='scale(2)'/></defs>" +
            "<clipPath id='c'><use href='#r' x='3' transform='translate(0,5)'/></clipPath>" +
            "<rect width='1' height='1' clip-path='url(#c)'/>");
        Assert.NotNull(group.Clip);
        var shape = Assert.Single(group.Clip.Shapes);
        Assert.Equal("F1 M0,0 H1 V1 H0 Z", shape.PathData);
        Assert.NotNull(shape.Transform);
        Assert.Equal((5d, 5d), shape.Transform.Value.Apply(1, 0));
    }

    [Fact]
    public void Parse_ClippedShape_IsWrappedInOneGroupWithItsTransformAndOpacity()
    {
        var group = ClippedGroup(
            "<clipPath id='c'><rect width='1' height='1'/></clipPath>" +
            "<rect width='1' height='1' transform='translate(3,4)' opacity='0.5' clip-path='url(#c)'/>");
        Assert.NotNull(group.Clip);
        Assert.NotNull(group.Transform);
        Assert.Equal((3d, 4d), group.Transform.Value.Apply(0, 0));
        Assert.Equal(0.5, group.Opacity);
        Assert.IsType<SvgShape>(Assert.Single(group.Children));
    }

    [Fact]
    public void Parse_ClipPathOnUse_IsOnItsGroup()
    {
        var group = ClippedGroup(
            "<defs><rect id='r' width='1' height='1'/></defs><clipPath id='c'><rect width='1' height='1'/></clipPath>" +
            "<use href='#r' x='1' clip-path='url(#c)'/>");
        Assert.NotNull(group.Clip);
        Assert.NotNull(group.Transform);
        Assert.IsType<SvgShape>(Assert.Single(group.Children));
    }

    [Theory]
    [InlineData("clip-path='inset(1px)'", "value:clip-path")]
    [InlineData("clip-path='circle(5px)'", "value:clip-path")]
    [InlineData("clip-path='url(#nope)'", "reference:nope")]
    [InlineData("clip-path='url(#r)'", "reference:r")]
    [InlineData("clip-rule='odd'", "value:clip-rule")]
    public void Parse_BadClipPathValue_GivesReason(string attribute, string reason)
    {
        Assert.Equal(reason, Reason(Document($"<rect id='r' width='1' height='1' {attribute}/>")));
    }

    [Theory]
    [InlineData("<clipPath id='c'/>")]
    [InlineData("<clipPath id='c'><rect width='1' height='1' display='none'/><rect width='0' height='1'/></clipPath>")]
    [InlineData("<clipPath id='c'><rect width='1' height='1' visibility='hidden'/></clipPath>")]
    public void Parse_ClipPathWithoutShapes_DropsClippedSubtree(string clipPath)
    {
        Assert.Empty(Root(clipPath + "<g clip-path='url(#c)'><rect width='10' height='10'/></g>").Children);
    }

    [Fact]
    public void Parse_ClipChildPaint_IsIgnored()
    {
        var group = ClippedGroup(
            "<clipPath id='c'><rect width='1' height='1' fill='none' stroke='red' opacity='0'/></clipPath>" +
            "<rect width='1' height='1' clip-path='url(#c)'/>");
        Assert.NotNull(group.Clip);
        Assert.Single(group.Clip.Shapes);
    }

    [Fact]
    public void Parse_ClipPathNone_IsNoClip()
    {
        var shape = Assert.IsType<SvgShape>(Assert.Single(Root("<rect width='1' height='1' clip-path='none'/>").Children));
        Assert.Equal("F1 M0,0 H1 V1 H0 Z", shape.PathData);
    }
}
