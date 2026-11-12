using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class SvgCssTests
{
    private static readonly SvgColor Red = new(255, 255, 0, 0);
    private static readonly SvgColor Blue = new(255, 0, 0, 255);

    private const string Rect = "<rect class='a' width='1' height='1'/>";

    private static string Sheet(string style, string body)
    {
        return Document($"<style>{style}</style>{body}", "width='10' height='10'");
    }

    /// <summary>The one shape in the drawing, inside any groups.</summary>
    private static SvgShape ShapeOf(string svgText)
    {
        var node = Assert.Single(Drawing(svgText).Root.Children);
        while (node is SvgGroup group)
        {
            node = Assert.Single(group.Children);
        }

        return Assert.IsType<SvgShape>(node);
    }

    /// <summary>The solid fill of the one shape in <paramref name="body" /> under the style sheet.</summary>
    private static SvgColor Fill(string style, string body)
    {
        return Assert.IsType<SvgSolidPaint>(ShapeOf(Sheet(style, body)).Fill).Color;
    }

    [Fact]
    public void Parse_CommentInsideADeclaration_SeparatesTokens()
    {
        var stroke = ShapeOf(Sheet("path{stroke-dasharray:1/**/2}", "<path d='M0 0h10' stroke='#000'/>")).Stroke;
        Assert.NotNull(stroke);
        Assert.Equal([1d, 2d], stroke.DashArray!);
    }

    [Fact]
    public void Parse_CommentInsideASelector_IsLeftOut()
    {
        Assert.Equal(Red, Fill("rect/**/.a/* note */{fill:red}", Rect));
    }

    [Fact]
    public void Parse_ClassSelector_Applies()
    {
        Assert.Equal(Red, Fill(".a{fill:red}", Rect));
    }

    [Fact]
    public void Parse_CssRule_BeatsPresentationAttribute()
    {
        Assert.Equal(Red, Fill("rect{fill:red}", "<rect width='1' height='1' fill='blue'/>"));
    }

    [Fact]
    public void Parse_InlineStyle_BeatsCssRule()
    {
        Assert.Equal(Blue, Fill(".a{fill:red}", "<rect class='a' style='fill:blue' width='1' height='1'/>"));
    }

    [Fact]
    public void Parse_IdSelector_BeatsClassSelector()
    {
        Assert.Equal(Red, Fill("#x{fill:red} .a{fill:blue}", "<rect id='x' class='a' width='1' height='1'/>"));
    }

    [Fact]
    public void Parse_EqualSpecificity_LaterRuleWins()
    {
        Assert.Equal(Blue, Fill(".a{fill:red} .a{fill:blue}", Rect));
    }

    [Fact]
    public void Parse_CompoundSelector_BeatsClassSelector()
    {
        Assert.Equal(Red, Fill("rect.a{fill:red} .a{fill:blue}", Rect));
    }

    [Fact]
    public void Parse_ElevenClasses_DoNotOutrankAnId()
    {
        Assert.Equal(Red,
            Fill("#x{fill:red} .a.a.a.a.a.a.a.a.a.a.a{fill:blue}", "<rect id='x' class='a' width='1' height='1'/>"));
    }

    [Fact]
    public void Parse_SelectorList_MatchesEachSelector()
    {
        Assert.Equal(Red, Fill(".a,.b{fill:red}", "<rect class='b' width='1' height='1'/>"));
    }

    [Fact]
    public void Parse_SeveralClasses_EachMatches()
    {
        Assert.Equal(Red, Fill(".b{fill:red}", "<rect class='a b' width='1' height='1'/>"));
    }

    [Fact]
    public void Parse_CommentsAndWhitespace_AreIgnored()
    {
        Assert.Equal(Red, Fill("/* c */ .a { fill : red ; }", Rect));
    }

    [Fact]
    public void Parse_CData_IsRead()
    {
        Assert.Equal(Red, Fill("<![CDATA[.a{fill:red}]]>", Rect));
    }

    [Fact]
    public void Parse_ClassOnGroup_IsInheritedByItsShape()
    {
        Assert.Equal(Red, Fill(".g{fill:red}", "<g class='g'><rect width='1' height='1'/></g>"));
    }

    [Fact]
    public void Parse_StyleAfterTheShape_StillApplies()
    {
        var shape = ShapeOf(Document(Rect + "<style>.a{fill:red}</style>", "width='10' height='10'"));
        Assert.Equal(new SvgSolidPaint(Red), shape.Fill);
    }

    [Fact]
    public void Parse_SeveralStyleElements_AreReadInDocumentOrder()
    {
        var shape = ShapeOf(Document(
            "<style>.a{fill:red}</style><defs><style>.a{fill:blue}</style></defs>" + Rect,
            "width='10' height='10'"));
        Assert.Equal(new SvgSolidPaint(Blue), shape.Fill);
    }

    [Fact]
    public void Parse_RootSvg_CanBeMatched()
    {
        Assert.Equal(Red, Fill("svg{fill:red}", "<rect width='1' height='1'/>"));
    }

    [Theory]
    [InlineData(".A{fill:red}", "<rect class='a' width='1' height='1'/>")]
    [InlineData("RECT{fill:red}", "<rect width='1' height='1'/>")]
    [InlineData("#X{fill:red}", "<rect id='x' width='1' height='1'/>")]
    [InlineData(".a{fill:red}", "<rect class='ab' width='1' height='1'/>")]
    public void Parse_MatchingIsCaseSensitiveAndByWholeToken(string style, string body)
    {
        Assert.Equal(SvgColor.Black, Fill(style, body));
    }

    [Theory]
    [InlineData(".a{}")]
    [InlineData(".a{ \n }")]
    [InlineData(".a{;}")]
    public void Parse_EmptyRuleBody_IsAccepted(string style)
    {
        Assert.Equal(SvgColor.Black, Fill(style, Rect));
    }

    [Theory]
    [InlineData(".a{fill:red;}")]
    [InlineData(".a{fill:red}")]
    [InlineData(".a{fill:blue;fill:red}")]
    [InlineData(".a/* x */.a{fill:red}")]
    [InlineData("<![CDATA[<!-- .a{fill:red} -->]]>")]
    [InlineData(".a{fill:url(\"#g;x}\") red}")]
    [InlineData(".a{FILL:red}")]
    public void Parse_Declarations_AreRead(string style)
    {
        Assert.Equal(Red, Fill(style, Rect));
    }

    [Theory]
    [InlineData("")]
    [InlineData("type='text/css'")]
    [InlineData("type=' TEXT/CSS '")]
    [InlineData("media='all'")]
    [InlineData("media=''")]
    [InlineData("media=' ALL '")]
    public void Parse_StyleTypeAndMedia_AreAccepted(string attributes)
    {
        var shape = ShapeOf(Document($"<style {attributes}>.a{{fill:red}}</style>" + Rect, "width='10' height='10'"));
        Assert.Equal(new SvgSolidPaint(Red), shape.Fill);
    }

    [Theory]
    [InlineData("type='text/less'")]
    [InlineData("type=''")]
    [InlineData("media='print'")]
    [InlineData("media='screen and (min-width: 1px)'")]
    public void Parse_OtherStyleTypeOrMedia_IsCss(string attributes)
    {
        Assert.Equal("css", Reason(Document($"<style {attributes}>.a{{fill:red}}</style>" + Rect)));
    }

    [Theory]
    [InlineData("g rect{fill:red}")]
    [InlineData("g>rect{fill:red}")]
    [InlineData("rect:hover{fill:red}")]
    [InlineData("[x]{fill:red}")]
    [InlineData("@media print{.a{fill:red}}")]
    [InlineData(".a{fill:red !important}")]
    [InlineData("*{fill:red}")]
    [InlineData("g+rect{fill:red}")]
    [InlineData("g~rect{fill:red}")]
    [InlineData("svg|rect{fill:red}")]
    [InlineData("@charset \"utf-8\";")]
    [InlineData("@namespace svg url(http://www.w3.org/2000/svg);")]
    [InlineData("@font-face{font-family:x}")]
    [InlineData("@import url(a.css);")]
    [InlineData(".a\\:b{fill:red}")]
    [InlineData(".a{fill:\\72 ed}")]
    [InlineData("[class='a']{fill:red}")]
    [InlineData(".a, rect:first-child{fill:red}")]
    [InlineData(".a,{fill:red}")]
    [InlineData(".{fill:red}")]
    [InlineData("#1{fill:red}")]
    [InlineData(".a{fill:red")]
    [InlineData(".a{fill:red}}")]
    [InlineData(".a{.b{fill:red}}")]
    [InlineData("fill:red")]
    [InlineData(".a{fill:red} x")]
    [InlineData(".a{fill:red};")]
    [InlineData(".a{fill}")]
    [InlineData(".a{:red}")]
    [InlineData(".a{fill:red /* open")]
    [InlineData(".a{fill:url(\"#g) red}")]
    public void Parse_CssBeyondSubset_IsUnsupported(string style)
    {
        Assert.Equal("css", Reason(Sheet(style, Rect)));
    }

    [Fact]
    public void Parse_ElementInsideStyle_IsCss()
    {
        Assert.Equal("css", Reason(Document("<style>.a{fill:red}<rect/></style>" + Rect)));
    }

    [Fact]
    public void Parse_WhitespaceBetweenCDataSections_SeparatesSelectors()
    {
        Assert.Equal("css", Reason(Sheet("<![CDATA[.a]]> <![CDATA[.a{fill:red}]]>", Rect)));
    }

    [Fact]
    public void Parse_StyleInsideClipPath_StaysElementClipPath()
    {
        Assert.Equal("element:clipPath",
            Reason(Document("<clipPath id='c'><style>.a{fill:red}</style></clipPath>" + Rect)));
    }

    [Fact]
    public void Parse_StyleInsideGradient_Applies()
    {
        Assert.Equal(Red, Fill("",
            "<defs><linearGradient id='g'><style>.a{fill:red}</style></linearGradient></defs>" + Rect));
    }

    [Theory]
    [InlineData(".a{filter:url(#f)}", "property:filter")]
    [InlineData(".a{transform:scale(2)}", "property:transform")]
    [InlineData(".a{width:10px}", "property:width")]
    [InlineData(".a{r:1}", "property:r")]
    [InlineData(".a{mix-blend-mode:multiply}", "property:mix-blend-mode")]
    [InlineData(".a{fill:lch(1 2 3)}", "value:fill")]
    public void Parse_RestrictedOrInvalidDeclarationOnMatch_GivesInlineStyleReason(string style, string reason)
    {
        Assert.Equal(reason, Reason(Sheet(style, Rect)));
    }

    [Theory]
    [InlineData(".b{filter:url(#f)}")]
    [InlineData("circle{transform:scale(2)}")]
    [InlineData("#nothing{fill:lch(1 2 3)}")]
    public void Parse_RestrictedDeclarationMatchingNothing_IsInert(string style)
    {
        Assert.Equal(SvgColor.Black, Fill(style, Rect));
    }

    [Fact]
    public void Parse_UnknownProperty_IsIgnoredAsInInlineStyle()
    {
        Assert.Equal(Red, Fill(".a{fill:red;font-size:20px}", Rect));
    }

    [Fact]
    public void Parse_GradientStops_TakeCss()
    {
        var paint = Assert.IsType<SvgLinearGradientPaint>(ShapeOf(Sheet(
            ".s{stop-color:red;stop-opacity:0.5} #last{stop-color:blue}",
            "<defs><linearGradient id='g'><stop class='s'/><stop id='last' offset='1' stop-color='lime'/>" +
            "</linearGradient></defs><rect width='1' height='1' fill='url(#g)'/>")).Fill);
        Assert.Equal(new SvgColor(128, 255, 0, 0), paint.Stops[0].Color);
        Assert.Equal(Blue, paint.Stops[1].Color);
    }

    [Fact]
    public void Parse_ClipPathChildren_TakeCss()
    {
        var group = Assert.IsType<SvgGroup>(Assert.Single(Drawing(Sheet(
            ".c{clip-rule:evenodd}",
            "<clipPath id='c'><rect class='c' width='5' height='5'/></clipPath>" +
            "<rect width='1' height='1' clip-path='url(#c)'/>")).Root.Children));
        // Even-odd path data has no F1 prefix.
        Assert.StartsWith("M", Assert.Single(group.Clip!.Shapes).PathData);
    }

    [Theory]
    [InlineData("#t{fill:red} .u{fill:blue}", "<rect id='t' width='1' height='1'/>")]
    [InlineData("#t{fill:red} #u{fill:blue}", "<rect id='t' width='1' height='1'/>")]
    [InlineData(".u{fill:red}", "<rect id='t' width='1' height='1'/>")]
    [InlineData("rect{fill:red} use{fill:blue}", "<rect id='t' width='1' height='1'/>")]
    [InlineData(".r{fill:red}", "<g id='t'><rect class='r' width='1' height='1'/></g>")]
    public void Parse_UseTarget_IsMatchedByItsOwnPlace(string style, string definition)
    {
        Assert.Equal(Red, Fill(style, $"<defs>{definition}</defs><use id='u' class='u' href='#t'/>"));
    }
}
