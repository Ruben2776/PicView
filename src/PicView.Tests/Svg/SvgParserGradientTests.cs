using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class SvgParserGradientTests
{
    private const string BlackToWhite =
        "<stop offset='0' stop-color='#000'/><stop offset='1' stop-color='#fff' stop-opacity='.5'/>";

    private static SvgPaint? Fill(string definitions, string fill = "url(#g)", string extra = "")
    {
        return Shape($"<defs>{definitions}</defs><rect width='10' height='10' fill='{fill}' {extra}/>").Fill;
    }

    private static SvgLinearGradientPaint Linear(string definitions, string extra = "")
    {
        return Assert.IsType<SvgLinearGradientPaint>(Fill(definitions, extra: extra));
    }

    private static SvgRadialGradientPaint Radial(string definitions)
    {
        return Assert.IsType<SvgRadialGradientPaint>(Fill(definitions));
    }

    private static string ReasonFor(string definitions)
    {
        return Reason(Document($"<defs>{definitions}</defs><rect width='10' height='10' fill='url(#g)'/>"))!;
    }

    [Fact]
    public void Parse_LinearGradient_HasDefaultsAndStops()
    {
        var paint = Linear($"<linearGradient id='g'>{BlackToWhite}</linearGradient>");
        Assert.Equal(0, paint.X1);
        Assert.Equal(0, paint.Y1);
        Assert.Equal(1, paint.X2);
        Assert.Equal(0, paint.Y2);
        Assert.Equal(SvgGradientUnits.ObjectBoundingBox, paint.Units);
        Assert.Equal(SvgSpreadMethod.Pad, paint.Spread);
        Assert.Null(paint.Transform);
        Assert.Equal(1, paint.Opacity);
        Assert.Equal(
            [new SvgGradientStop(0, new SvgColor(0xFF, 0, 0, 0)), new SvgGradientStop(1, new SvgColor(0x80, 0xFF, 0xFF, 0xFF))],
            paint.Stops);
    }

    [Fact]
    public void Parse_UserSpaceGradient_UsesUserUnits()
    {
        var paint = Linear(
            $"<linearGradient id='g' gradientUnits='userSpaceOnUse' x1='10' x2='90'>{BlackToWhite}</linearGradient>");
        Assert.Equal(SvgGradientUnits.UserSpaceOnUse, paint.Units);
        Assert.Equal(10, paint.X1);
        Assert.Equal(90, paint.X2);
        Assert.Equal(0, paint.Y1);
        Assert.Equal(0, paint.Y2);
    }

    [Fact]
    public void Parse_UserSpaceGradientPercent_UsesViewport()
    {
        var paint = Assert.IsType<SvgLinearGradientPaint>(Assert.IsType<SvgShape>(Assert.Single(Drawing(Document(
            $"<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' y2='50%'>{BlackToWhite}</linearGradient></defs>" +
            "<rect width='10' height='10' fill='url(#g)'/>", "width='200' height='100'")).Root.Children)).Fill);
        Assert.Equal(200, paint.X2);
        Assert.Equal(50, paint.Y2);
    }

    [Theory]
    [InlineData("0.25")]
    [InlineData("25%")]
    public void Parse_BoundingBoxCoordinate_IsFraction(string x1)
    {
        Assert.Equal(0.25, Linear($"<linearGradient id='g' x1='{x1}'>{BlackToWhite}</linearGradient>").X1);
    }

    [Fact]
    public void Parse_GradientTransform_IsSet()
    {
        var paint = Linear(
            $"<linearGradient id='g' gradientUnits='userSpaceOnUse' gradientTransform='rotate(90)'>{BlackToWhite}" +
            "</linearGradient>");
        Assert.NotNull(paint.Transform);
        var (x, y) = paint.Transform.Value.Apply(1, 0);
        Assert.Equal(0, x, 9);
        Assert.Equal(1, y, 9);
    }

    [Fact]
    public void Parse_IdentityGradientTransform_IsNull()
    {
        Assert.Null(Linear($"<linearGradient id='g' gradientTransform='translate(0)'>{BlackToWhite}</linearGradient>")
            .Transform);
    }

    [Theory]
    [InlineData("pad", SvgSpreadMethod.Pad)]
    [InlineData("reflect", SvgSpreadMethod.Reflect)]
    [InlineData("repeat", SvgSpreadMethod.Repeat)]
    public void Parse_SpreadMethod_IsRead(string value, SvgSpreadMethod expected)
    {
        Assert.Equal(expected, Linear($"<linearGradient id='g' spreadMethod='{value}'>{BlackToWhite}</linearGradient>")
            .Spread);
    }

    [Fact]
    public void Parse_HrefChain_InheritsStopsAndUnsetAttributes()
    {
        var paint = Linear(
            $"<linearGradient id='a' x2='0.5' spreadMethod='repeat'>{BlackToWhite}</linearGradient>" +
            "<linearGradient id='m' href='#a' y1='0.2'/>" +
            "<linearGradient id='g' href='#m' x2='0.3'/>");
        Assert.Equal(2, paint.Stops.Count);
        Assert.Equal(new SvgColor(0x80, 0xFF, 0xFF, 0xFF), paint.Stops[1].Color);
        Assert.Equal(0.3, paint.X2);
        Assert.Equal(0.2, paint.Y1);
        Assert.Equal(SvgSpreadMethod.Repeat, paint.Spread);
    }

    [Fact]
    public void Parse_HrefWithOwnStops_KeepsOwnStops()
    {
        var paint = Linear(
            $"<linearGradient id='a'>{BlackToWhite}</linearGradient>" +
            "<linearGradient id='g' href='#a'><stop stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient>");
        Assert.Equal(new SvgColor(255, 255, 0, 0), paint.Stops[0].Color);
    }

    [Fact]
    public void Parse_RadialHrefToLinear_InheritsStopsOnly()
    {
        var paint = Radial(
            $"<linearGradient id='a' x1='0.9' gradientUnits='userSpaceOnUse'>{BlackToWhite}</linearGradient>" +
            "<radialGradient id='g' href='#a'/>");
        Assert.Equal(2, paint.Stops.Count);
        Assert.Equal(SvgGradientUnits.UserSpaceOnUse, paint.Units);
        Assert.Equal(50, paint.Cx);
    }

    [Theory]
    [InlineData("<linearGradient id='g' href='#g'/>", "reference:g")]
    [InlineData("<linearGradient id='g' href='#a'/><linearGradient id='a' href='#g'/>", "reference:g")]
    [InlineData("<linearGradient id='g' href='#nope'/>", "reference:nope")]
    [InlineData("<linearGradient id='g' href='#r'/><rect id='r'/>", "reference:r")]
    [InlineData("<linearGradient id='g' href='other.svg#a'/>", "reference:other.svg#a")]
    public void Parse_BadGradientHref_IsReference(string definitions, string reason)
    {
        Assert.Equal(reason, ReasonFor(definitions));
    }

    [Fact]
    public void Parse_RadialGradient_FocalDefaultsToCenter()
    {
        var paint = Radial($"<radialGradient id='g' fx='25%'>{BlackToWhite}</radialGradient>");
        Assert.Equal(0.5, paint.Cx);
        Assert.Equal(0.5, paint.Cy);
        Assert.Equal(0.5, paint.R);
        Assert.Equal(0.25, paint.Fx);
        Assert.Equal(0.5, paint.Fy);
    }

    [Fact]
    public void Parse_RadialGradientNonzeroFr_IsProperty()
    {
        Assert.Equal("property:fr", ReasonFor($"<radialGradient id='g' fr='0.1'>{BlackToWhite}</radialGradient>"));
    }

    [Fact]
    public void Parse_RadialGradientZeroFr_IsAccepted()
    {
        Assert.Equal(0.5, Radial($"<radialGradient id='g' fr='0'>{BlackToWhite}</radialGradient>").R);
    }

    [Fact]
    public void Parse_OneStop_IsSolid()
    {
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 255, 0, 0)),
            Fill("<linearGradient id='g'><stop offset='0.3' stop-color='red'/></linearGradient>"));
    }

    [Fact]
    public void Parse_NoStops_IsNoPaint()
    {
        Assert.Null(Fill("<linearGradient id='g'/>", "url(#g) red"));
    }

    [Fact]
    public void Parse_MissingServerWithFallback_IsFallback()
    {
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 255, 0, 0)), Fill("", "url(#missing) red"));
    }

    [Fact]
    public void Parse_MissingServerWithoutFallback_IsNoPaint()
    {
        Assert.Null(Fill("", "url(#missing)"));
    }

    [Fact]
    public void Parse_ServerThatIsNotAGradient_IsFallback()
    {
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 255, 0, 0)),
            Fill("<rect id='g' width='1' height='1'/>", "url(#g) red"));
    }

    [Fact]
    public void Parse_ServerInOtherFile_IsReference()
    {
        Assert.Equal("reference:a.svg#g", Reason(Document("<rect width='1' height='1' fill='url(a.svg#g)'/>")));
    }

    [Fact]
    public void Parse_QuotedUrlWithCurrentColorFallback_IsCurrentColor()
    {
        Assert.Equal(new SvgSolidPaint(SvgColor.Black), Fill("", "url(\"#missing\") currentColor"));
    }

    [Fact]
    public void Parse_FillOpacity_SetsGradientOpacity()
    {
        var paint = Linear($"<linearGradient id='g'>{BlackToWhite}</linearGradient>", "fill-opacity='0.5'");
        Assert.Equal(0.5, paint.Opacity);
        Assert.Equal(new SvgColor(0xFF, 0, 0, 0), paint.Stops[0].Color);
    }

    [Fact]
    public void Parse_StrokeGradient_UsesStrokeOpacity()
    {
        var stroke = Shape(
            $"<defs><linearGradient id='g' gradientUnits='userSpaceOnUse'>{BlackToWhite}</linearGradient></defs>" +
            "<rect width='10' height='10' stroke='url(#g)' stroke-opacity='0.25'/>").Stroke;
        Assert.NotNull(stroke);
        Assert.Equal(0.25, Assert.IsType<SvgLinearGradientPaint>(stroke.Paint).Opacity);
    }

    [Fact]
    public void Parse_Stops_AreClampedAndSorted()
    {
        var paint = Linear(
            "<linearGradient id='g'><stop offset='-1'/><stop offset='50%'/><stop offset='0.2'/><stop offset='1.5'/>" +
            "</linearGradient>");
        Assert.Equal([0d, 0.5, 0.5, 1d], paint.Stops.Select(s => s.Offset));
    }

    [Fact]
    public void Parse_StopStyleAttribute_IsRead()
    {
        var paint = Linear(
            "<linearGradient id='g'><stop style='stop-color:red;stop-opacity:0.5'/><stop offset='1'/></linearGradient>");
        Assert.Equal(new SvgColor(0x80, 255, 0, 0), paint.Stops[0].Color);
        Assert.Equal(SvgColor.Black, paint.Stops[1].Color);
    }

    [Fact]
    public void Parse_StopCurrentColor_UsesGradientContext()
    {
        var paint = Linear(
            "<linearGradient id='g' color='blue'><stop stop-color='currentColor'/><stop offset='1'/></linearGradient>",
            "color='red'");
        Assert.Equal(new SvgColor(255, 0, 0, 255), paint.Stops[0].Color);
    }

    [Theory]
    [InlineData("<linearGradient id='g'><stop offset='half'/></linearGradient>", "value:offset")]
    [InlineData("<linearGradient id='g'><stop stop-color='nope'/></linearGradient>", "value:stop-color")]
    [InlineData("<linearGradient id='g'><stop stop-opacity='nope'/></linearGradient>", "value:stop-opacity")]
    [InlineData("<linearGradient id='g' gradientUnits='user'/>", "value:gradientUnits")]
    [InlineData("<linearGradient id='g' spreadMethod='Pad'/>", "value:spreadMethod")]
    [InlineData("<linearGradient id='g' gradientTransform='spin(1)'/>", "value:gradientTransform")]
    [InlineData("<linearGradient id='g' x1='1px'/>", "value:x1")]
    [InlineData("<radialGradient id='g' r='-1'/>", "value:r")]
    public void Parse_BadGradientValue_GivesReason(string definitions, string reason)
    {
        Assert.Equal(reason, ReasonFor(definitions));
    }

    [Fact]
    public void Parse_RadialZeroRadius_IsLastStopSolid()
    {
        Assert.Equal(new SvgSolidPaint(new SvgColor(0x80, 0xFF, 0xFF, 0xFF)),
            Fill($"<radialGradient id='g' r='0'>{BlackToWhite}</radialGradient>"));
    }

    [Fact]
    public void Parse_LinearCoincidentUserSpacePoints_IsLastStopSolid()
    {
        Assert.Equal(new SvgSolidPaint(new SvgColor(0x40, 0xFF, 0xFF, 0xFF)),
            Fill($"<linearGradient id='g' gradientUnits='userSpaceOnUse' x1='5' x2='5'>{BlackToWhite}</linearGradient>",
                extra: "fill-opacity='0.5'"));
    }

    [Fact]
    public void Parse_StrayStopAndNonStopGradientChild_AreIgnoredAndNotDrawn()
    {
        var root = Root(
            "<stop offset='0.5' stop-color='blue'/>" +
            "<linearGradient id='g'><stop stop-color='red'/><rect width='5' height='5'/>" +
            "<stop offset='1' stop-color='lime'/></linearGradient>" +
            "<rect width='1' height='1' fill='url(#g)'/>");
        var paint = Assert.IsType<SvgLinearGradientPaint>(Assert.IsType<SvgShape>(Assert.Single(root.Children)).Fill);
        Assert.Equal(
            [new SvgGradientStop(0, new SvgColor(255, 255, 0, 0)), new SvgGradientStop(1, new SvgColor(255, 0, 255, 0))],
            paint.Stops);
    }

    [Fact]
    public void Parse_GradientOutsideDefs_IsUsable()
    {
        var root = Root($"<linearGradient id='g'>{BlackToWhite}</linearGradient><rect width='1' height='1' fill='url(#g)'/>");
        Assert.IsType<SvgLinearGradientPaint>(Assert.IsType<SvgShape>(Assert.Single(root.Children)).Fill);
    }

    [Fact]
    public void Parse_BoundingBoxGradientWithTransform_IsGradientTransform()
    {
        Assert.Equal("gradient:transform",
            ReasonFor($"<linearGradient id='g' gradientTransform='translate(0.5 0)'>{BlackToWhite}</linearGradient>"));
    }

    [Fact]
    public void Parse_BoundingBoxOneStopGradientWithTransform_IsSolid()
    {
        Assert.Equal(new SvgSolidPaint(new SvgColor(255, 255, 0, 0)),
            Fill("<linearGradient id='g' gradientTransform='scale(2)'><stop stop-color='red'/></linearGradient>"));
    }

    [Theory]
    [InlineData("<rect width='10' height='10' fill='none' stroke='url(#g)'/>")]
    [InlineData("<path d='M0 5h10' stroke='url(#g)' stroke-width='4'/>")]
    public void Parse_BoundingBoxGradientOnStroke_IsGradientStroke(string shape)
    {
        Assert.Equal("gradient:stroke",
            Reason(Document($"<defs><linearGradient id='g'>{BlackToWhite}</linearGradient></defs>{shape}")));
    }

    [Fact]
    public void Parse_BoundingBoxGradientOnZeroWidthStroke_IsAccepted()
    {
        Assert.Null(Shape($"<defs><linearGradient id='g'>{BlackToWhite}</linearGradient></defs>" +
                          "<rect width='10' height='10' stroke='url(#g)' stroke-width='0'/>").Stroke);
    }

    [Theory]
    [InlineData("fx='1.2'")]
    [InlineData("fx='1'")]
    [InlineData("fx='0.1' fy='0.1'")]
    [InlineData("gradientUnits='userSpaceOnUse' cx='50' cy='50' r='10' fx='65'")]
    public void Parse_RadialFocalOnOrOutsideTheCircle_IsGradientFocal(string attributes)
    {
        Assert.Equal("gradient:focal",
            ReasonFor($"<radialGradient id='g' {attributes}>{BlackToWhite}</radialGradient>"));
    }
}
