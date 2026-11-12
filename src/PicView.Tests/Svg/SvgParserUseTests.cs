using System.Diagnostics;
using System.Globalization;
using System.Text;
using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class SvgParserUseTests
{
    private const string XLinkRoot =
        "width='100' height='100' xmlns:xlink='http://www.w3.org/1999/xlink'";

    private static readonly SvgColor Red = new(255, 255, 0, 0);

    [Fact]
    public void Parse_Use_TranslatesAndInheritsStyle()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root("<defs><rect id='r' width='10' height='10'/></defs><use href='#r' x='5' y='7' fill='red'/>")
                .Children));
        Assert.NotNull(group.Transform);
        Assert.Equal((5d, 7d), group.Transform.Value.Apply(0, 0));
        var shape = Assert.IsType<SvgShape>(Assert.Single(group.Children));
        Assert.Equal(new SvgSolidPaint(Red), shape.Fill);
    }

    [Fact]
    public void Parse_XLinkHref_WorksLikeHref()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(Drawing(Document(
            "<defs><rect id='r' width='10' height='10'/></defs><use xlink:href='#r' x='5' y='7' fill='red'/>",
            XLinkRoot)).Root.Children));
        Assert.NotNull(group.Transform);
        Assert.Equal((5d, 7d), group.Transform.Value.Apply(0, 0));
        var shape = Assert.IsType<SvgShape>(Assert.Single(group.Children));
        Assert.Equal(new SvgSolidPaint(Red), shape.Fill);
    }

    [Fact]
    public void Parse_HrefAndXLinkHref_PlainHrefWins()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(Drawing(Document(
            "<defs><rect id='a' width='1' height='1'/><rect id='b' width='2' height='2'/></defs>" +
            "<use xlink:href='#b' href='#a'/>",
            XLinkRoot)).Root.Children));
        Assert.Equal("F1 M0,0 H1 V1 H0 Z", Assert.IsType<SvgShape>(Assert.Single(group.Children)).PathData);
    }

    [Fact]
    public void Parse_UseOfMissingId_IsReference()
    {
        Assert.Equal("reference:nope", Reason(Document("<use href='#nope'/>")));
    }

    [Fact]
    public void Parse_UseOfOtherFile_IsReference()
    {
        Assert.Equal("reference:other.svg#r", Reason(Document("<use href='other.svg#r'/>")));
    }

    [Theory]
    [InlineData("<linearGradient id='t'/><use href='#t'/>")]
    [InlineData("<clipPath id='t'/><use href='#t'/>")]
    [InlineData("<defs id='t'/><use href='#t'/>")]
    [InlineData("<rect id='r' width='1' height='1'/><use id='t' href='#r'/><use href='#t'/>")]
    public void Parse_UseOfNonShapeOrGroup_IsReference(string body)
    {
        Assert.Equal("reference:t", Reason(Document(body)));
    }

    [Fact]
    public void Parse_UseOfRootSvg_IsReference()
    {
        Assert.Equal("reference:s", Reason(Document("<use href='#s'/>", "id='s' width='100' height='100'")));
    }

    [Fact]
    public void Parse_UseOfOwnAncestor_IsReference()
    {
        Assert.Equal("reference:a", Reason(Document("<g id='a'><g><use href='#a'/></g></g>")));
    }

    [Fact]
    public void Parse_MutualUseCycle_IsReference()
    {
        Assert.Equal("reference:a", Reason(Document(
            "<defs><g id='a'><use href='#b'/></g><g id='b'><use href='#a'/></g></defs><use href='#a'/>")));
    }

    [Fact]
    public void Parse_UseDoublingAtEveryLevel_IsExpansion()
    {
        var text = new StringBuilder("<defs><g id='g0'><rect width='1' height='1'/></g>");
        for (var i = 1; i <= 25; i++)
        {
            text.Append(CultureInfo.InvariantCulture, $"<g id='g{i}'><use href='#g{i - 1}'/><use href='#g{i - 1}'/></g>");
        }

        text.Append("</defs><use href='#g25'/>");
        var svg = Document(text.ToString());

        var stopwatch = Stopwatch.StartNew();
        var reason = Reason(svg);
        stopwatch.Stop();

        Assert.Equal("expansion", reason);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"took {stopwatch.Elapsed}");
    }

    [Theory]
    [InlineData("<rect width='1' height='1' display='none'/>")]
    [InlineData("<rect/>")]
    public void Parse_UseDoublingOfLeavesThatDrawNothing_IsExpansion(string leaf)
    {
        var text = new StringBuilder("<defs><g id='g0'>");
        for (var i = 0; i < 1000; i++)
        {
            text.Append(leaf);
        }

        text.Append("</g>");
        for (var i = 1; i <= 25; i++)
        {
            text.Append(CultureInfo.InvariantCulture, $"<g id='g{i}'><use href='#g{i - 1}'/><use href='#g{i - 1}'/></g>");
        }

        text.Append("</defs><use href='#g25'/>");
        var svg = Document(text.ToString());

        var stopwatch = Stopwatch.StartNew();
        var reason = Reason(svg);
        stopwatch.Stop();

        Assert.Equal("expansion", reason);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"took {stopwatch.Elapsed}");
    }

    [Fact]
    public void Parse_UseChain300Deep_IsDepth()
    {
        var text = new StringBuilder("<defs><rect id='g0' width='1' height='1'/>");
        for (var i = 1; i <= 300; i++)
        {
            text.Append(CultureInfo.InvariantCulture, $"<g id='g{i}'><use href='#g{i - 1}'/></g>");
        }

        text.Append("</defs><use href='#g300'/>");
        Assert.Equal("depth", Reason(Document(text.ToString())));
    }

    [Fact]
    public void Parse_UseTransform_AppliesAfterTranslation()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root("<defs><rect id='r' width='1' height='1'/></defs><use href='#r' x='5' transform='scale(2)'/>")
                .Children));
        Assert.NotNull(group.Transform);
        Assert.Equal((10d, 0d), group.Transform.Value.Apply(0, 0));
        Assert.Equal((12d, 0d), group.Transform.Value.Apply(1, 0));
    }

    [Fact]
    public void Parse_UseOpacity_IsOnItsGroup()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root("<defs><rect id='r' width='1' height='1'/></defs><use href='#r' opacity='0.5'/>").Children));
        Assert.Equal(0.5, group.Opacity);
        Assert.Null(group.Transform);
        Assert.IsType<SvgShape>(Assert.Single(group.Children));
    }

    [Fact]
    public void Parse_UseTargetOwnAttribute_WinsOverInherited()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root("<defs><rect id='r' width='1' height='1' fill='blue'/></defs><use href='#r' fill='red'/>")
                .Children));
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 0, 0, 255)),
            Assert.IsType<SvgShape>(Assert.Single(group.Children)).Fill);
    }

    [Fact]
    public void Parse_UseOfGroup_HoldsTheGroup()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(
            Root("<defs><g id='g' transform='translate(1,2)'><rect width='1' height='1'/></g></defs>" +
                 "<use href='#g' stroke='red'/>").Children));
        var inner = Assert.IsType<SvgGroup>(Assert.Single(group.Children));
        Assert.NotNull(inner.Transform);
        Assert.Equal((1d, 2d), inner.Transform.Value.Apply(0, 0));
        var shape = Assert.IsType<SvgShape>(Assert.Single(inner.Children));
        Assert.NotNull(shape.Stroke);
        Assert.Equal(new SvgSolidPaint(Red), shape.Stroke.Paint);
    }

    [Fact]
    public void Parse_SameTargetUsedTwice_IsNotACycle()
    {
        var root = Root("<defs><rect id='r' width='1' height='1'/></defs><use href='#r'/><use href='#r' x='2'/>");
        Assert.Equal(2, root.Children.Count);
    }

    [Fact]
    public void Parse_UseWithoutHref_DrawsNothing()
    {
        Assert.Empty(Root("<use x='1'/>").Children);
    }

    [Fact]
    public void Parse_Definitions_AreNotDrawn()
    {
        Assert.Empty(Root(
            "<defs><rect width='1' height='1'/><g><rect width='1' height='1'/></g></defs>" +
            "<linearGradient><stop/></linearGradient><radialGradient/><clipPath><rect width='1' height='1'/></clipPath>")
            .Children);
    }

    [Fact]
    public void Parse_DuplicateIds_FirstWins()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(Root(
            "<defs><rect id='r' width='1' height='1'/><rect id='r' width='2' height='2'/><rect id='R' width='3' height='3'/></defs>" +
            "<use href='#r'/>").Children));
        Assert.Equal("F1 M0,0 H1 V1 H0 Z", Assert.IsType<SvgShape>(Assert.Single(group.Children)).PathData);
    }
}
