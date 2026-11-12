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
    [InlineData("<defs/>", "element:defs")]
    [InlineData("<use href='#a'/>", "element:use")]
    [InlineData("<linearGradient/>", "element:linearGradient")]
    [InlineData("<radialGradient/>", "element:radialGradient")]
    [InlineData("<stop/>", "element:stop")]
    [InlineData("<clipPath/>", "element:clipPath")]
    [InlineData("<style>rect{}</style>", "element:style")]
    [InlineData("<rect width='1' height='1' clip-path='url(#c)'/>", "property:clip-path")]
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
