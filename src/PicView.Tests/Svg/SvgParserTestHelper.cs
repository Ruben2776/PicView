using PicView.Core.Svg;

namespace PicView.Tests.Svg;

internal static class SvgParserTestHelper
{
    public const string Namespace = "http://www.w3.org/2000/svg";

    /// <summary>Wraps <paramref name="body" /> in a 100 by 100 svg element in the SVG namespace.</summary>
    public static string Document(string body, string rootAttributes = "width='100' height='100'")
    {
        return $"<svg xmlns='{Namespace}' {rootAttributes}>{body}</svg>";
    }

    public static SvgDrawing Drawing(string svgText, SvgParseOptions? options = null)
    {
        var result = SvgParser.Parse(svgText, options);
        Assert.Null(result.UnsupportedReason);
        Assert.NotNull(result.Drawing);
        return result.Drawing;
    }

    public static SvgGroup Root(string body, SvgParseOptions? options = null)
    {
        return Drawing(Document(body), options).Root;
    }

    public static SvgShape Shape(string body, SvgParseOptions? options = null)
    {
        var child = Assert.Single(Root(body, options).Children);
        return Assert.IsType<SvgShape>(child);
    }

    public static SvgColor Solid(string body, SvgParseOptions? options = null)
    {
        return Assert.IsType<SvgSolidPaint>(Shape(body, options).Fill).Color;
    }

    public static string? Reason(string svgText)
    {
        var result = SvgParser.Parse(svgText);
        if (result.UnsupportedReason is not null)
        {
            Assert.Null(result.Drawing);
        }

        return result.UnsupportedReason;
    }
}
