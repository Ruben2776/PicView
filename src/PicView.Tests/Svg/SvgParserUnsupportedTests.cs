using System.Globalization;
using System.Text;
using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class SvgParserUnsupportedTests
{
    [Theory]
    [InlineData("<text>a</text>", "element:text")]
    [InlineData("<image href='a.png'/>", "element:image")]
    [InlineData("<rect width='1' height='1' filter='url(#f)'/>", "property:filter")]
    [InlineData("<rect width='1' height='1' style='mix-blend-mode:multiply'/>", "property:mix-blend-mode")]
    [InlineData("<rect width='-1' height='1'/>", "value:width")]
    [InlineData("<rect width='1' height='1' fill='lch(1 2 3)'/>", "value:fill")]
    [InlineData("<path d='M0 0 X'/>", "value:d")]
    public void Parse_Unsupported_GivesReason(string body, string reason)
    {
        Assert.Equal(reason, Reason(Document(body)));
    }

    [Theory]
    [InlineData("<svg width='1' height='1'/>", "element:svg")]
    [InlineData("<g><svg width='1' height='1'/></g>", "element:svg")]
    [InlineData("<use href='#a'/>", "reference:a")]
    [InlineData("<rect width='1' height='1' clip-path='url(#c)'/>", "reference:c")]
    [InlineData("<rect width='1' height='1' mask='url(#m)'/>", "property:mask")]
    [InlineData("<rect width='1' height='1' marker='url(#m)'/>", "property:marker")]
    [InlineData("<rect width='1' height='1' marker-start='url(#m)'/>", "property:marker-start")]
    [InlineData("<rect width='1' height='1' marker-mid='url(#m)'/>", "property:marker-mid")]
    [InlineData("<rect width='1' height='1' marker-end='url(#m)'/>", "property:marker-end")]
    [InlineData("<rect width='1' height='1' vector-effect='non-scaling-stroke'/>", "property:vector-effect")]
    [InlineData("<rect width='1' height='1' paint-order='stroke'/>", "property:paint-order")]
    [InlineData("<rect width='1' height='1' transform-origin='center'/>", "property:transform-origin")]
    [InlineData("<rect width='1' height='1' style='transform-box:fill-box'/>", "property:transform-box")]
    [InlineData("<rect width='1' height='1' style='transform:scale(2)'/>", "property:transform")]
    [InlineData("<rect width='1' height='1' style='fill:red !important'/>", "css")]
    public void Parse_NotYetHandled_GivesReason(string body, string reason)
    {
        Assert.Equal(reason, Reason(Document(body)));
    }

    [Theory]
    [InlineData("rotate:45deg", "property:rotate")]
    [InlineData("scale:2", "property:scale")]
    [InlineData("translate:10px", "property:translate")]
    [InlineData("all:initial", "property:all")]
    [InlineData("offset-path:path('M0 0h10')", "property:offset-path")]
    [InlineData("mask-image:linear-gradient(red,blue)", "property:mask-image")]
    [InlineData("mask-mode:luminance", "property:mask-mode")]
    [InlineData("mask-repeat:no-repeat", "property:mask-repeat")]
    [InlineData("mask-position:center", "property:mask-position")]
    [InlineData("mask-clip:border-box", "property:mask-clip")]
    [InlineData("mask-origin:border-box", "property:mask-origin")]
    [InlineData("mask-size:cover", "property:mask-size")]
    [InlineData("mask-composite:add", "property:mask-composite")]
    [InlineData("mask-type:alpha", "property:mask-type")]
    [InlineData("mask-border:none", "property:mask-border")]
    [InlineData("-webkit-mask:none", "property:-webkit-mask")]
    [InlineData("-webkit-mask-image:none", "property:-webkit-mask-image")]
    [InlineData("clip:rect(0 1px 1px 0)", "property:clip")]
    [InlineData("background:red", "property:background")]
    [InlineData("background-color:red", "property:background-color")]
    [InlineData("background-image:linear-gradient(red,blue)", "property:background-image")]
    public void Parse_CssPropertyABrowserApplies_GivesReason(string declaration, string reason)
    {
        Assert.Equal(reason, Reason(Document($"<rect width='1' height='1' style=\"{declaration}\"/>")));
        Assert.Equal(reason, Reason(Document($"<style>rect {{ {declaration} }}</style><rect width='1' height='1'/>")));
    }

    [Theory]
    [InlineData("background:red", "property:background")]
    [InlineData("background-color:#fff", "property:background-color")]
    public void Parse_BackgroundOnTheRoot_GivesReason(string declaration, string reason)
    {
        Assert.Equal(reason, Reason(Document("", $"width='1' height='1' style='{declaration}'")));
    }

    [Theory]
    [InlineData("clip:auto")]
    [InlineData("background:none")]
    [InlineData("background:transparent")]
    [InlineData("background-color:transparent")]
    [InlineData("background-image:none")]
    public void Parse_NeutralBrowserProperty_IsAccepted(string declaration)
    {
        Assert.Single(Root($"<rect width='1' height='1' style='{declaration}'/>").Children);
    }

    [Theory]
    [InlineData("systemLanguage='en'", "property:systemLanguage")]
    [InlineData("requiredExtensions='http://example.com'", "property:requiredExtensions")]
    [InlineData("requiredFeatures='http://www.w3.org/TR/SVG11/feature#Shape'", "property:requiredFeatures")]
    public void Parse_ConditionalAttribute_GivesReason(string attribute, string reason)
    {
        Assert.Equal(reason, Reason(Document($"<g {attribute}><rect width='1' height='1'/></g>")));
        Assert.Equal(reason, Reason(Document($"<defs><linearGradient id='g' {attribute}/></defs>")));
        Assert.Equal(reason, Reason(Document("", $"width='1' height='1' {attribute}")));
    }

    [Theory]
    [InlineData("<?xml-stylesheet href='a.css' type='text/css'?>" +
                "<svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'/>")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'>" +
                "<?xml-stylesheet href='a.css'?></svg>")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'>" +
                "<title><?xml-stylesheet href='a.css'?></title></svg>")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'>" +
                "<style><?xml-stylesheet href='a.css'?></style></svg>")]
    public void Parse_XmlStyleSheetInstruction_IsCss(string svgText)
    {
        Assert.Equal("css", Reason(svgText));
    }

    [Fact]
    public void Parse_OtherProcessingInstruction_IsIgnored()
    {
        Assert.Single(Drawing("<?other data?>" + Document("<?inkscape x?><rect width='1' height='1'/>")).Root
            .Children);
    }

    [Theory]
    [InlineData("<title><style>rect{fill:red}</style></title>")]
    [InlineData("<desc><style>rect{fill:red}</style></desc>")]
    [InlineData("<metadata><x><style>rect{fill:red}</style></x></metadata>")]
    [InlineData("<f:x xmlns:f='urn:f'><style>rect{fill:red}</style></f:x>")]
    [InlineData("<f:x xmlns:f='urn:f'><f:style>rect{fill:red}</f:style></f:x>")]
    [InlineData("<h:style xmlns:h='http://www.w3.org/1999/xhtml'>rect{fill:red}</h:style>")]
    [InlineData("<style title='alternate'>rect{fill:red}</style>")]
    public void Parse_StyleSheetABrowserMayApplyDifferently_IsCss(string body)
    {
        Assert.Equal("css", Reason(Document(body + "<rect width='1' height='1'/>")));
    }

    [Fact]
    public void Parse_StyleWithEmptyTitle_IsRead()
    {
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 255, 0, 0)),
            Shape("<style title=''>rect{fill:red}</style><rect width='1' height='1'/>").Fill);
    }

    [Theory]
    [InlineData("<rect style='width:10px;height:10px'/>", "property:width")]
    [InlineData("<rect width='1' style='height:10px'/>", "property:height")]
    [InlineData("<rect width='1' height='1' style='x:1'/>", "property:x")]
    [InlineData("<rect width='1' height='1' style='y:1'/>", "property:y")]
    [InlineData("<rect width='1' height='1' style='rx:1'/>", "property:rx")]
    [InlineData("<rect width='1' height='1' style='ry:1'/>", "property:ry")]
    [InlineData("<circle style='r:5'/>", "property:r")]
    [InlineData("<circle r='5' style='cx:1'/>", "property:cx")]
    [InlineData("<circle r='5' style='cy:1'/>", "property:cy")]
    [InlineData("<path style=\"d:path('M0 0h10')\"/>", "property:d")]
    public void Parse_GeometryPropertyInCss_GivesReason(string body, string reason)
    {
        Assert.Equal(reason, Reason(Document(body)));
    }

    [Fact]
    public void Parse_GeometryAttributes_AreHandled()
    {
        Assert.Equal("F1 M1,2 H11 V12 H1 Z",
            Assert.IsType<SvgShape>(Assert.Single(
                Root("<rect x='1' y='2' width='10' height='10' rx='0'/>").Children)).PathData);
    }

    [Fact]
    public void Parse_PathLength_GivesReason()
    {
        Assert.Equal("property:pathLength", Reason(Document("<path pathLength='10' d='M0 0h10'/>")));
    }

    [Theory]
    [InlineData("filter='none'")]
    [InlineData("mask='none'")]
    [InlineData("marker='none' marker-start='none' marker-mid='none' marker-end='none'")]
    [InlineData("clip-path='none'")]
    [InlineData("vector-effect='none'")]
    [InlineData("style='mix-blend-mode:normal;paint-order:Normal'")]
    public void Parse_NeutralValue_IsAccepted(string attributes)
    {
        Assert.Single(Root($"<rect width='1' height='1' {attributes}/>").Children);
    }

    [Theory]
    [InlineData("<g transform='scale(1e300) scale(1e300)'><rect width='1' height='1'/></g>", "value:transform")]
    [InlineData("<rect width='1' height='1' transform='matrix(1e308 0 0 1 0 0) translate(1e308)'/>",
        "value:transform")]
    [InlineData("<defs><rect id='r' width='1' height='1'/></defs><use href='#r' x='1e308' transform='translate(1e308)'/>",
        "value:transform")]
    [InlineData("<clipPath id='c' transform='scale(1e300)'><rect width='1' height='1' transform='scale(1e300)'/>" +
                "</clipPath><rect width='1' height='1' clip-path='url(#c)'/>", "value:transform")]
    [InlineData("<linearGradient id='g' gradientUnits='userSpaceOnUse' gradientTransform='scale(1e300) scale(1e300)'>" +
                "<stop/><stop offset='1'/></linearGradient><rect width='1' height='1' fill='url(#g)'/>", "value:gradientTransform")]
    [InlineData("<linearGradient id='g' gradientUnits='userSpaceOnUse' gradientTransform='scale(0)'>" +
                "<stop/><stop offset='1'/></linearGradient><rect width='1' height='1' fill='url(#g)'/>", "value:gradientTransform")]
    [InlineData("<linearGradient id='g' gradientUnits='userSpaceOnUse' gradientTransform='matrix(1 2 2 4 0 0)'>" +
                "<stop/><stop offset='1'/></linearGradient><rect width='1' height='1' fill='url(#g)'/>", "value:gradientTransform")]
    [InlineData("<path d='M0 0h1' stroke='#000' stroke-width='1e-300' stroke-dasharray='1e10 1'/>",
        "value:stroke-dasharray")]
    [InlineData("<path d='M0 0h1' stroke='#000' stroke-width='1e-300' stroke-dasharray='1e-300' " +
                "stroke-dashoffset='1e10'/>", "value:stroke-dashoffset")]
    public void Parse_NonFiniteOrSingularMatrixOrDashes_GivesReason(string body, string reason)
    {
        Assert.Equal(reason, Reason(Document(body)));
    }

    [Theory]
    [InlineData("scale(0)")]
    [InlineData("scale(1 0)")]
    [InlineData("matrix(1 2 2 4 5 6)")]
    public void Parse_SingularTransform_DropsTheSubtree(string transform)
    {
        var root = Root($"<g transform='{transform}'><rect width='1' height='1'/></g>" +
                        $"<rect width='1' height='1' transform='{transform}'/>" +
                        "<defs><rect id='r' width='1' height='1'/></defs>" +
                        $"<use href='#r' transform='{transform}'/>");
        Assert.Empty(root.Children);
    }

    [Fact]
    public void Parse_HugeWidth_IsSize()
    {
        Assert.Equal("size", Reason(Document("", "width='1e300' height='10'")));
    }

    [Theory]
    [InlineData("width='10000001' height='10'")]
    [InlineData("width='10' height='10000001'")]
    [InlineData("viewBox='0 0 20000000 1'")]
    public void Parse_ViewportAboveTenMillionPixels_IsSize(string rootAttributes)
    {
        Assert.Equal("size", Reason(Document("", rootAttributes)));
    }

    [Fact]
    public void Parse_ViewportOfTenMillionPixels_IsSupported()
    {
        Assert.Equal(10_000_000, Drawing(Document("", "width='10000000' height='1'")).Width);
    }

    [Fact]
    public void Parse_ViewBoxTransformNotFinite_IsViewBox()
    {
        Assert.Equal("value:viewBox", Reason(Document("", "width='1000' height='1000' viewBox='1e300 0 1e-300 1e-300'")));
    }

    [Fact]
    public void Parse_TruncatedXml_IsXml()
    {
        Assert.Equal("xml", Reason("<svg"));
    }

    [Theory]
    [InlineData("<html xmlns='http://www.w3.org/2000/svg'/>")]
    [InlineData("<svg width='1' height='1'/>")]
    public void Parse_RootNotSvgInSvgNamespace_IsXml(string svgText)
    {
        Assert.Equal("xml", Reason(svgText));
    }

    [Fact]
    public void Parse_NoSize_IsSize()
    {
        Assert.Equal("size", Reason($"<svg xmlns='{Namespace}'/>"));
    }

    [Fact]
    public void Parse_ZeroWidth_IsSize()
    {
        Assert.Equal("size", Reason(Document("", "width='0' height='10'")));
    }

    [Fact]
    public void Parse_OverflowingWidth_IsSize()
    {
        Assert.Equal("size", Reason(Document("", "width='1e999' height='10'")));
    }

    [Fact]
    public void Parse_TenThousandNestedGroups_IsDepth()
    {
        var text = new StringBuilder();
        for (var i = 0; i < 10_000; i++)
        {
            text.Append("<g>");
        }

        for (var i = 0; i < 10_000; i++)
        {
            text.Append("</g>");
        }

        Assert.Equal("depth", Reason(Document(text.ToString())));
    }

    [Fact]
    public void Parse_RootWith255NestedGroups_256ElementsDeep_Parses()
    {
        Assert.Single(Root(NestedGroups(255)).Children);
    }

    [Fact]
    public void Parse_RootWith256NestedGroups_257ElementsDeep_IsDepth()
    {
        Assert.Equal("depth", Reason(Document(NestedGroups(256))));
    }

    private static string NestedGroups(int count)
    {
        var text = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            text.Append("<g>");
        }

        for (var i = 0; i < count; i++)
        {
            text.Append("</g>");
        }

        return text.ToString();
    }

    [Fact]
    public void Parse_FilterNone_IsAccepted()
    {
        Assert.Single(Root("<rect width='1' height='1' filter='none'/>").Children);
    }

    [Fact]
    public void Parse_NonRenderingContent_IsIgnored()
    {
        var root = Drawing(
            $"<svg xmlns='{Namespace}' xmlns:inkscape='http://www.inkscape.org/namespaces/inkscape' " +
            "width='10' height='10' inkscape:version='1.0'>" +
            "<title>t</title><desc>d</desc><metadata><text>x</text></metadata>" +
            "<inkscape:grid><text>y</text></inkscape:grid>" +
            "<animate attributeName='x'/><set attributeName='x'/>" +
            "<animateTransform attributeName='transform'/><animateMotion/>" +
            "<rect width='1' height='1' data-x='1' inkscape:label='a'/></svg>").Root;
        Assert.IsType<SvgShape>(Assert.Single(root.Children));
    }

    [Fact]
    public void Parse_DoctypeWithoutEntities_Parses()
    {
        var root = Drawing(
            "<?xml version='1.0'?>\n" +
            "<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n" +
            Document("<rect width='1' height='1'/>")).Root;
        Assert.Single(root.Children);
    }

    [Fact]
    public void Parse_UnderDanishCulture_ReadsDecimals()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");
            var drawing = Drawing(Document("<rect width='1' height='1' fill-opacity='.5'/>", "width='1.5' height='1'"));
            Assert.Equal(1.5, drawing.Width);
            var shape = Assert.IsType<SvgShape>(Assert.Single(drawing.Root.Children));
            Assert.Equal(new SvgSolidPaint(new SvgColor(128, 0, 0, 0)), shape.Fill);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
