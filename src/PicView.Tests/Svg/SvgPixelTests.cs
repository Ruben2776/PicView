using System.Xml.Linq;
using Avalonia.Media;
using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

/// <summary>
/// Renders SVGs through the parser and the builder with real Skia and checks the pixels against what SVG specifies.
/// Pixels are sampled at least 2 px inside a shape so anti-aliased edges do not count.
/// </summary>
[Collection("Sequential")]
public class SvgPixelTests
{
    private const int Tolerance = 12;

    private const string BlackToWhite = "<stop offset='0' stop-color='#000'/><stop offset='1' stop-color='#fff'/>";

    private static readonly Color Red = Color.FromRgb(255, 0, 0);
    private static readonly Color Green = Color.FromRgb(0, 128, 0);

    private static string Svg(int width, int height, string body)
    {
        return Document(body, $"width='{width}' height='{height}'");
    }

    private static void AssertColor(Color expected, Color actual, int tolerance = Tolerance)
    {
        var close = Math.Abs(expected.A - actual.A) <= tolerance && Math.Abs(expected.R - actual.R) <= tolerance &&
                    Math.Abs(expected.G - actual.G) <= tolerance && Math.Abs(expected.B - actual.B) <= tolerance;
        Assert.True(close, $"Expected {expected} ±{tolerance}, got {actual}");
    }

    private static void AssertTransparent(Color actual)
    {
        Assert.True(actual.A <= Tolerance, $"Expected transparent, got {actual}");
    }

    private static void AssertPainted(Color actual)
    {
        Assert.True(actual.A >= 255 - Tolerance, $"Expected opaque, got {actual}");
    }

    private static void AssertNear(double expected, byte actual, int tolerance = Tolerance)
    {
        Assert.True(Math.Abs(expected - actual) <= tolerance, $"Expected {expected:0.#} ±{tolerance}, got {actual}");
    }

    [AvaloniaFact]
    public void Fill_RedRectOnLeftHalf_PaintsOnlyTheRect()
    {
        var pixels = SvgRender.Pixels(Svg(10, 10, "<rect width='5' height='10' fill='red'/>"));
        AssertColor(Red, pixels.At(2, 5));
        AssertTransparent(pixels.At(7, 5));
    }

    [AvaloniaFact]
    public void ViewBox_HalfSize_ScalesContentByTwo()
    {
        var pixels = SvgRender.Pixels(Document("<rect width='5' height='10' fill='red'/>",
            "width='20' height='20' viewBox='0 0 10 10'"));
        AssertColor(Red, pixels.At(8, 10));
        AssertTransparent(pixels.At(12, 10));
    }

    [AvaloniaFact]
    public void Clip_CircleAtCenter_KeepsOnlyTheCircle()
    {
        var pixels = SvgRender.Pixels(Svg(10, 10,
            "<defs><clipPath id='c'><circle cx='5' cy='5' r='3'/></clipPath></defs>" +
            "<g clip-path='url(#c)'><rect width='10' height='10' fill='green'/></g>"));
        AssertColor(Green, pixels.At(5, 5));
        AssertTransparent(pixels.At(0, 0));
    }

    [AvaloniaFact]
    public void Clip_ShapeWithTransform_MovesTheClip()
    {
        var pixels = SvgRender.Pixels(Svg(10, 10,
            "<defs><clipPath id='c'><rect width='5' height='5' transform='translate(5 5)'/></clipPath></defs>" +
            "<g clip-path='url(#c)'><rect width='10' height='10' fill='green'/></g>"));
        AssertColor(Green, pixels.At(7, 7));
        AssertTransparent(pixels.At(2, 2));
        AssertTransparent(pixels.At(7, 2));
    }

    [AvaloniaFact]
    public void Clip_TwoShapes_KeepsTheirUnion()
    {
        var pixels = SvgRender.Pixels(Svg(10, 10,
            "<defs><clipPath id='c'><rect width='4' height='10'/><rect x='6' width='4' height='10'/></clipPath></defs>" +
            "<g clip-path='url(#c)'><rect width='10' height='10' fill='green'/></g>"));
        AssertColor(Green, pixels.At(2, 5));
        AssertColor(Green, pixels.At(8, 5));
        AssertTransparent(pixels.At(5, 5));
    }

    [AvaloniaTheory]
    [InlineData("nonzero", true)]
    [InlineData("evenodd", false)]
    public void FillRule_NestedSameDirectionSquares_FillsCenterOnlyForNonZero(string rule, bool centerFilled)
    {
        var pixels = SvgRender.Pixels(Svg(20, 20,
            $"<path fill='red' fill-rule='{rule}' d='M0,0 H20 V20 H0 Z M5,5 H15 V15 H5 Z'/>"));
        AssertColor(Red, pixels.At(2, 10));
        if (centerFilled)
        {
            AssertColor(Red, pixels.At(10, 10));
        }
        else
        {
            AssertTransparent(pixels.At(10, 10));
        }
    }

    [AvaloniaFact]
    public void Gradient_BoundingBox_RunsAcrossTheRect()
    {
        var pixels = SvgRender.Pixels(Svg(100, 10,
            "<defs><linearGradient id='g'>" + BlackToWhite + "</linearGradient></defs>" +
            "<rect width='100' height='10' fill='url(#g)'/>"));
        Assert.True(pixels.At(5, 5).R < 40, $"Got {pixels.At(5, 5)}");
        Assert.True(pixels.At(95, 5).R > 215, $"Got {pixels.At(95, 5)}");
    }

    [AvaloniaFact]
    public void Mapping1_UserSpaceGradient_IsMeasuredFromTheDrawingOrigin()
    {
        var pixels = SvgRender.Pixels(Svg(100, 10,
            "<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' x2='100'>" + BlackToWhite +
            "</linearGradient></defs>" +
            "<rect x='50' width='50' height='10' fill='url(#g)'/>"));
        AssertNear(140, pixels.At(55, 5).R, 20);
    }

    [AvaloniaFact]
    public void Mapping2_BoundingBoxGradientTransform_IsUnsupported()
    {
        // Avalonia applies the brush transform in user units, not bounding-box units: translate(0.5 0) on a
        // 100 px box moved the gradient by half a pixel (red 64 at x=25, where SVG gives 0).
        Assert.Equal("gradient:transform", Reason(Svg(100, 10,
            "<defs><linearGradient id='g' gradientTransform='translate(0.5 0)'>" + BlackToWhite +
            "</linearGradient></defs>" +
            "<rect width='100' height='10' fill='url(#g)'/>")));
    }

    [AvaloniaFact]
    public void Mapping3_BoundingBoxGradientOnStroke_IsUnsupported()
    {
        // SVG measures the gradient on the fill box x 20..80, so x=22 is 2/60 of the way: about 9. Avalonia
        // measured it on the stroked bounds x 10..90 (red 40, where 12/80 of the way is 38).
        Assert.Equal("gradient:stroke", Reason(Svg(100, 30,
            "<defs><linearGradient id='g'>" + BlackToWhite + "</linearGradient></defs>" +
            "<rect x='20' y='5' width='60' height='20' fill='none' stroke='url(#g)' stroke-width='20'/>")));
    }

    [AvaloniaFact]
    public void Gradient_BoundingBoxOnZeroHeightStroke_IsUnsupported()
    {
        // SVG paints nothing here; Avalonia painted the stroke (red 133 at the middle).
        Assert.Equal("gradient:stroke", Reason(Svg(20, 10,
            "<defs><linearGradient id='g'>" + BlackToWhite + "</linearGradient></defs>" +
            "<path d='M0 5h20' fill='none' stroke='url(#g)' stroke-width='4'/>")));
    }

    [AvaloniaFact]
    public void Gradient_BoundingBoxOnOffsetRect_StartsAtTheRect()
    {
        // The box is x 50..100: pixel center 55.5 is 5.5/50 of the way, red ≈ 28.
        var pixels = SvgRender.Pixels(Svg(100, 10,
            "<defs><linearGradient id='g'>" + BlackToWhite + "</linearGradient></defs>" +
            "<rect x='50' width='50' height='10' fill='url(#g)'/>"));
        AssertNear(28, pixels.At(55, 5).R);
    }

    [AvaloniaFact]
    public void Gradient_UserSpaceTransform_IsAboutTheUserSpaceOrigin()
    {
        // scale(0.5 1) about the origin makes the gradient run x 0..50, so the rect at x 50..100 is all white.
        // About the rect's corner (Avalonia's default) it would run x 25..75: red ≈ 181 at x=60.
        var pixels = SvgRender.Pixels(Svg(100, 10,
            "<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' x2='100' " +
            "gradientTransform='scale(0.5 1)'>" + BlackToWhite + "</linearGradient></defs>" +
            "<rect x='50' width='50' height='10' fill='url(#g)'/>"));
        AssertNear(255, pixels.At(60, 5).R);
    }

    [AvaloniaFact]
    public void RadialGradient_UserSpace_IsMeasuredFromTheDrawingOrigin()
    {
        // Center (0,5), r = 100: pixel center 75.5 is 0.755 out, red ≈ 193. Measured from the rect's corner
        // (center at x=50) it would be 0.255 out, red ≈ 65.
        var pixels = SvgRender.Pixels(Svg(100, 10,
            "<defs><radialGradient id='g' gradientUnits='userSpaceOnUse' cx='0' cy='5' r='100'>" + BlackToWhite +
            "</radialGradient></defs>" +
            "<rect x='50' width='50' height='10' fill='url(#g)'/>"));
        AssertNear(193, pixels.At(75, 5).R);
    }

    [AvaloniaFact]
    public void RadialGradient_FocalOutsideTheCircle_IsUnsupported()
    {
        Assert.Equal("gradient:focal", Reason(Svg(100, 100,
            "<defs><radialGradient id='g' fx='1.2'>" + BlackToWhite + "</radialGradient></defs>" +
            "<rect width='100' height='100' fill='url(#g)'/>")));
    }

    [AvaloniaFact]
    public void RadialGradient_FocalInside_FollowsTheFocalCone()
    {
        // SVG: the gradient value t at P is the t where P lies on the circle with center F + t(C - F) and radius t·r.
        // F = (30,50), C = (50,50), r = 50 (bounding box 100x100). Pixel centers sit at +0.5.
        // (50,50): P - F = (20.5, 0.5), (20.5 - 20t)² + 0.25 = (50t)² gives t ≈ 0.293, red ≈ 75.
        // (80,50): P - F = (50.5, 0.5) gives t ≈ 0.721, red ≈ 184.
        // (10,50): P - F = (-19.5, 0.5) gives 19.5 + 20t = 50t, t ≈ 0.650, red ≈ 166.
        // Without the focal point (concentric) these would be about 0, 156 and 201.
        var pixels = SvgRender.Pixels(Svg(100, 100,
            "<defs><radialGradient id='g' fx='0.3'>" + BlackToWhite + "</radialGradient></defs>" +
            "<rect width='100' height='100' fill='url(#g)'/>"));
        AssertNear(75, pixels.At(50, 50).R);
        AssertNear(184, pixels.At(80, 50).R);
        AssertNear(166, pixels.At(10, 50).R);
    }

    [AvaloniaFact]
    public void RadialGradient_BoundingBoxOnWideRect_IsAnEllipse()
    {
        // r = 50% of a 100x50 box: radius 50 across, 25 down. Pixel centers 2.5 px in from the edge middles:
        // (2,25) is 47.5/50 = 0.95 out, red ≈ 242; (50,2) is 22.5/25 = 0.9 out, red ≈ 230.
        // A circle of radius 50 would give (50,2) 0.45 out, red ≈ 115.
        var pixels = SvgRender.Pixels(Svg(100, 50,
            "<defs><radialGradient id='g'>" + BlackToWhite + "</radialGradient></defs>" +
            "<rect width='100' height='50' fill='url(#g)'/>"));
        AssertNear(242, pixels.At(2, 25).R);
        AssertNear(230, pixels.At(50, 2).R);
    }

    [AvaloniaTheory]
    // x1=0 x2=50 over 100 px: at pixel center 87.5, t = 1.75. reflect: 0.25 (red ≈ 64); repeat: 0.75 (red ≈ 191).
    [InlineData("reflect", 64)]
    [InlineData("repeat", 191)]
    public void Gradient_SpreadMethod_ContinuesPastTheEnd(string spread, int expectedRed)
    {
        var pixels = SvgRender.Pixels(Svg(100, 10,
            $"<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' x2='50' spreadMethod='{spread}'>" +
            BlackToWhite + "</linearGradient></defs>" +
            "<rect width='100' height='10' fill='url(#g)'/>"));
        AssertNear(expectedRed, pixels.At(87, 5).R);
    }

    [AvaloniaFact]
    public void Gradient_FillOpacityHalf_GivesHalfAlpha()
    {
        var pixels = SvgRender.Pixels(Svg(100, 10,
            "<defs><linearGradient id='g'>" + BlackToWhite + "</linearGradient></defs>" +
            "<rect width='100' height='10' fill='url(#g)' fill-opacity='0.5'/>"));
        AssertNear(128, pixels.At(50, 5).A);
    }

    [AvaloniaFact]
    public void GroupOpacity_OverlappingShapes_AppliesToTheGroupAsAWhole()
    {
        var pixels = SvgRender.Pixels(Svg(30, 10,
            "<g opacity='0.5'><rect width='20' height='10' fill='red'/><rect x='10' width='20' height='10' fill='red'/></g>"));
        var outside = pixels.At(5, 5);
        var overlap = pixels.At(15, 5);
        AssertNear(128, outside.A);
        AssertColor(outside, overlap, 2);
    }

    [AvaloniaFact]
    public void Stroke_UnderNonUniformScale_ScalesWithTheGroup()
    {
        // stroke-width 2 under scale(2,1): a vertical line at x=10 covers x 18..22, a horizontal line at y=10
        // covers y 9..11.
        var pixels = SvgRender.Pixels(Svg(40, 20,
            "<g transform='scale(2,1)'><path d='M10,0 V8' stroke='red' stroke-width='2'/>" +
            "<path d='M12,10 H18' stroke='red' stroke-width='2'/></g>"));
        AssertPainted(pixels.At(18, 4));
        AssertPainted(pixels.At(21, 4));
        // The edges fall on pixel boundaries, so the neighbors just outside get no anti-aliasing.
        AssertTransparent(pixels.At(17, 4));
        AssertTransparent(pixels.At(22, 4));

        AssertPainted(pixels.At(30, 9));
        AssertPainted(pixels.At(30, 10));
        AssertTransparent(pixels.At(30, 8));
        AssertTransparent(pixels.At(30, 11));
    }

    [AvaloniaFact]
    public void Gradient_BoundingBoxFillWithStroke_UsesTheFillBox()
    {
        // The fill box is x 50..150; the stroked bounds would be x 30..170. Pixel center 71.5 is 21.5/100 of the
        // way on the fill box (red ≈ 55) and 41.5/140 on the stroked bounds (red ≈ 76). The stroke covers only
        // x 30..70 on the left, so (71,50) shows the fill.
        var pixels = SvgRender.Pixels(Svg(200, 100,
            "<defs><linearGradient id='g'>" + BlackToWhite + "</linearGradient></defs>" +
            "<rect x='50' y='10' width='100' height='80' fill='url(#g)' stroke='#000' stroke-width='40'/>"));
        AssertNear(55, pixels.At(71, 50).R);
    }

    [AvaloniaFact]
    public void Gradient_UserSpaceOnStroke_IsMeasuredFromTheDrawingOrigin()
    {
        // x1=0 x2=100 on a line from x=50: pixel center 75.5 gives red ≈ 193; from the line's start it would be 65.
        var pixels = SvgRender.Pixels(Svg(100, 10,
            "<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' x2='100'>" + BlackToWhite +
            "</linearGradient></defs>" +
            "<path d='M50,5 H100' stroke='url(#g)' stroke-width='4'/>"));
        AssertNear(193, pixels.At(75, 5).R);
    }

    [AvaloniaFact]
    public void GroupOpacity_StrokedChild_FadesTheWholeStrokeOnce()
    {
        // The stroke reaches x 5..35, outside the fill x 10..30; fill and stroke overlap at x 10..15.
        var pixels = SvgRender.Pixels(Svg(40, 40,
            "<g opacity='0.5'><rect x='10' y='10' width='20' height='20' fill='red' stroke='red' " +
            "stroke-width='10'/></g>"));
        var strokeOnly = pixels.At(6, 20);
        AssertNear(128, strokeOnly.A);
        AssertColor(strokeOnly, pixels.At(12, 20), 2);
        AssertColor(strokeOnly, pixels.At(20, 20), 2);
    }

    [AvaloniaFact]
    public void GroupOpacity_TransformedChild_IsFadedWhereTheChildIsDrawn()
    {
        var pixels = SvgRender.Pixels(Svg(80, 20,
            "<g opacity='0.5'><rect width='10' height='10' fill='red'/>" +
            "<g transform='translate(50 5)'><rect width='20' height='10' fill='red'/></g></g>"));
        AssertNear(128, pixels.At(5, 5).A);
        AssertNear(128, pixels.At(65, 10).A);
    }

    [AvaloniaFact]
    public void RadialGradient_FocalInsideOnWideRect_FollowsTheFocalCone()
    {
        // In bounding-box units on a 100x50 rect: F = (0.3,0.5), C = (0.5,0.5), r = 0.5; P = ((x+.5)/100, (y+.5)/50).
        // t solves |P - F - t(C - F)| = t·r.
        // (80,25): P - F = (.505,.01), t ≈ 0.721, red ≈ 184.
        // (10,25): P - F = (-.195,.01), t ≈ 0.650, red ≈ 166.
        // (50,5):  P - F = (.205,-.39), .21t² + .082t - .194 = 0, t ≈ 0.786, red ≈ 200.
        var pixels = SvgRender.Pixels(Svg(100, 50,
            "<defs><radialGradient id='g' fx='0.3'>" + BlackToWhite + "</radialGradient></defs>" +
            "<rect width='100' height='50' fill='url(#g)'/>"));
        AssertNear(184, pixels.At(80, 25).R);
        AssertNear(166, pixels.At(10, 25).R);
        AssertNear(200, pixels.At(50, 5).R);
    }

    [AvaloniaFact]
    public void Clip_ThreeShapes_KeepsTheirUnion()
    {
        var pixels = SvgRender.Pixels(Svg(30, 10,
            "<defs><clipPath id='c'><rect width='6' height='10'/><rect x='12' width='6' height='10'/>" +
            "<rect x='24' width='6' height='10'/></clipPath></defs>" +
            "<g clip-path='url(#c)'><rect width='30' height='10' fill='green'/></g>"));
        AssertColor(Green, pixels.At(3, 5));
        AssertColor(Green, pixels.At(15, 5));
        AssertColor(Green, pixels.At(27, 5));
        AssertTransparent(pixels.At(9, 5));
        AssertTransparent(pixels.At(21, 5));
    }

    [AvaloniaFact]
    public void SmoothCubic_AfterLine_StartsFromTheCurrentPoint()
    {
        // SVG takes the S's first control point at (60,50), so the curve bulges down to y ≈ 68. Mirroring the
        // cubic's (40,90) about (60,50) would bulge it up through (72,46) instead.
        var pixels = SvgRender.Pixels(Svg(100, 100, "<path d='M10 50C10 90 40 90 40 50L60 50S90 90 90 50Z'/>"));
        AssertTransparent(pixels.At(72, 46));
        AssertPainted(pixels.At(75, 60));
    }

    [AvaloniaFact]
    public void SmoothQuadratic_AfterLine_IsAStraightLine()
    {
        // SVG takes the T's control point at (60,50): a straight line back along y 50. Mirroring the quadratic's
        // (25,90) about (60,50) would bulge it up to (95,10), covering (80,42).
        var pixels = SvgRender.Pixels(Svg(100, 100, "<path d='M10 50Q25 90 40 50L60 50T90 50Z'/>"));
        AssertTransparent(pixels.At(80, 42));
        AssertPainted(pixels.At(25, 60));
    }

    [AvaloniaFact]
    public void Clip_AThousandShapes_RendersTheirUnionAndWritesAxaml()
    {
        var svg = SvgParserClipTests.ManyClipShapes(1000);
        var pixels = SvgRender.Pixels(svg);
        AssertColor(Green, pixels.At(0, 2));
        AssertColor(Green, pixels.At(1000, 2));
        AssertColor(Green, pixels.At(1998, 2));
        AssertTransparent(pixels.At(1001, 2));
        AssertTransparent(pixels.At(1999, 2));

        var text = SvgToXamlConverter.Convert(svg, out var reason);
        Assert.Null(reason);
        Assert.Equal(1000, XDocument.Parse(text!).Descendants("StreamGeometry").Count());
    }

    [AvaloniaFact]
    public void Dashes_FourTwoAtWidthTwo_PaintFourAndSkipTwo()
    {
        var pixels = SvgRender.Pixels(Svg(20, 10,
            "<path d='M0,5 H20' stroke='red' stroke-width='2' stroke-dasharray='4 2'/>"));
        AssertPainted(pixels.At(1, 5));
        AssertPainted(pixels.At(3, 5));
        AssertTransparent(pixels.At(5, 5));
        AssertPainted(pixels.At(7, 5));
    }

    [AvaloniaTheory]
    // A line from (20,20) to (40,20), width 8. (17,20) lies 2.5 px past the end; (16,16) is outside a round cap.
    [InlineData("butt", false, false)]
    [InlineData("round", true, false)]
    [InlineData("square", true, true)]
    public void LineCap_MapsAsNamed(string cap, bool pastEndPainted, bool cornerPainted)
    {
        var pixels = SvgRender.Pixels(Svg(60, 40,
            $"<path d='M20,20 H40' stroke='red' stroke-width='8' stroke-linecap='{cap}'/>"));
        AssertPaintedIf(pastEndPainted, pixels.At(17, 20));
        AssertPaintedIf(cornerPainted, pixels.At(16, 16));
    }

    [AvaloniaTheory]
    // A right angle at (40,40), width 20, outer corner toward (50,50). (46,45) is inside the round join and past
    // the bevel; (48,48) is only inside the miter.
    [InlineData("miter", true, true)]
    [InlineData("round", true, false)]
    [InlineData("bevel", false, false)]
    public void LineJoin_MapsAsNamed(string join, bool nearPainted, bool cornerPainted)
    {
        var pixels = SvgRender.Pixels(Svg(80, 80,
            $"<path d='M5,40 H40 V5' fill='none' stroke='red' stroke-width='20' stroke-linejoin='{join}'/>"));
        AssertPaintedIf(nearPainted, pixels.At(46, 45));
        AssertPaintedIf(cornerPainted, pixels.At(48, 48));
    }

    [AvaloniaTheory]
    // A 20° angle at (150,60), width 10: the miter is 1/sin(10°) ≈ 5.76 widths long, so SVG's default limit 4
    // bevels it. The miter tip reaches (178.4,65); pixel (170,63) lies inside the miter and past the bevel.
    [InlineData("", false)]
    [InlineData("stroke-miterlimit='6'", true)]
    public void MiterJoin_SharpAngle_IsBeveledAtSvgDefaultLimit(string limit, bool tipPainted)
    {
        var pixels = SvgRender.Pixels(Svg(200, 100,
            $"<path d='M10,60 L150,60 L18.44,12.12' fill='none' stroke='red' stroke-width='10' {limit}/>"));
        AssertPaintedIf(tipPainted, pixels.At(170, 63));
    }

    private static void AssertPaintedIf(bool painted, Color actual)
    {
        if (painted)
        {
            AssertPainted(actual);
        }
        else
        {
            AssertTransparent(actual);
        }
    }
}
