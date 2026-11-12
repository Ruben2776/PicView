using System.Globalization;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using PicView.Core.Svg;
using PicView.Core.Svg.Parsing;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

/// <summary>
/// Checks that every value the AXAML writer emits is accepted by Avalonia's own parser for that property, and that
/// path data in the model's written form means to Avalonia what it means to SVG.
/// </summary>
[Collection("Sequential")]
public class AxamlValueTests
{
    public static TheoryData<string> GoldenSvgs => new()
    {
        SvgToXamlConverterTests.FillAndStrokeSvg,
        Document(SvgToXamlConverterTests.GroupSvg),
        Document(SvgToXamlConverterTests.SolidAlphaSvg),
        Document(SvgToXamlConverterTests.LinearSvg),
        Document(SvgToXamlConverterTests.RadialSvg),
        Document(SvgToXamlConverterTests.DashSvg),
        Document(SvgToXamlConverterTests.MiterSvg),
        Document(SvgToXamlConverterTests.OneClipSvg),
        Document(SvgToXamlConverterTests.TwoClipSvg),
        // Round-trip numbers: long decimals in a matrix, path data, dashes and percent points.
        Document("<defs><linearGradient id='g' x1='0.3333333333333333' x2='0.07'><stop offset='0.1'/></linearGradient>" +
                 "</defs><g transform='scale(0.0004)'><rect width='10.123456789' height='1e-7' fill='url(#g)' " +
                 "stroke='#000' stroke-width='3' stroke-dasharray='1 2'/></g>")
    };

    [AvaloniaTheory]
    [MemberData(nameof(GoldenSvgs))]
    public void Write_EveryValue_ParsesWithAvaloniasParser(string svgText)
    {
        var text = SvgToXamlConverter.Convert(svgText, out var reason);
        Assert.Null(reason);
        var document = XDocument.Parse(text!);

        foreach (var element in document.Descendants())
        {
            foreach (var attribute in element.Attributes())
            {
                ParseValue(attribute.Name.LocalName, attribute.Value);
            }

            if (string.Equals(element.Name.LocalName, "StreamGeometry", StringComparison.Ordinal))
            {
                StreamGeometry.Parse(element.Value);
            }
        }
    }

    private static void ParseValue(string name, string value)
    {
        switch (name)
        {
            case "Viewbox":
                Rect.Parse(value);
                break;
            case "Brush" or "Color":
                Color.Parse(value);
                break;
            case "Geometry":
                StreamGeometry.Parse(value);
                break;
            case "Matrix":
                Matrix.Parse(value);
                break;
            case "StartPoint" or "EndPoint" or "Center" or "GradientOrigin" or "TransformOrigin":
                RelativePoint.Parse(value);
                break;
            case "RadiusX" or "RadiusY":
                RelativeScalar.Parse(value);
                break;
            case "LineCap":
                Enum.Parse<PenLineCap>(value);
                break;
            case "LineJoin":
                Enum.Parse<PenLineJoin>(value);
                break;
            case "SpreadMethod":
                Enum.Parse<GradientSpreadMethod>(value);
                break;
            case "GeometryCombineMode":
                Enum.Parse<GeometryCombineMode>(value);
                break;
            case "FillRule":
                Enum.Parse<FillRule>(value);
                break;
            case "Opacity" or "Thickness" or "MiterLimit" or "Offset":
                double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                break;
            case "Dashes":
                foreach (var dash in value.Split(',', StringSplitOptions.TrimEntries))
                {
                    double.Parse(dash, NumberStyles.Float, CultureInfo.InvariantCulture);
                }

                break;
            default:
                Assert.Fail($"No Avalonia parser is mapped for attribute {name}=\"{value}\"");
                break;
        }
    }

    /// <summary>
    /// SVG path data, and the bounds SVG gives it (x, y, width, height).
    /// </summary>
    public static TheoryData<string, double, double, double, double> PathForms => new()
    {
        // Numbers
        { "M.5 0 L10 10", 0.5, 0, 9.5, 10 },
        { "M5. 0 L10 10", 5, 0, 5, 10 },
        { "M+5 0 L10 10", 5, 0, 5, 10 },
        { "M1e2 0 L0 10", 0, 0, 100, 10 },
        { "M1E2 0 L0 10", 0, 0, 100, 10 },
        { "M-.5 0 L10 10", -0.5, 0, 10.5, 10 },
        // Compact arc flags: radius 1 from (0,0) to (.5,.5), center (-.41,.91); the arc stays inside its end points.
        { "M0 0 a1 1 0 01.5.5", 0, 0, 0.5, 0.5 },
        // Implicit repeats
        { "M0 0 10 0 10 10", 0, 0, 10, 10 },
        { "m1 1 9 0 0 9", 1, 1, 9, 9 },
        { "M0 0 L10 0 10 10", 0, 0, 10, 10 },
        { "M0 0 l10 0 0 10", 0, 0, 10, 10 },
        { "M0 0 c0 0 10 0 10 0 0 0 0 10 0 10", 0, 0, 10, 10 },
        // A relative move after z starts from the closed subpath's start (0,0), so the second subpath is at x 20..25.
        { "M0 0 h10 v10 z m20 0 h5 v5 z", 0, 0, 25, 10 },
        // Lines along the axes
        { "M1 2 H5 V7 h-2 v-3", 1, 2, 4, 5 },
        // Cubic (0,0)(0,10)(10,10)(10,0) peaks at y 7.5; the smooth one mirrors it down to -7.5.
        { "M0 0 C0 10 10 10 10 0 S20 -10 20 0", 0, -7.5, 20, 15 },
        { "M0 0 c0 10 10 10 10 0 s10 -10 10 0", 0, -7.5, 20, 15 },
        // Quadratic (0,0)(5,10)(10,0) peaks at y 5; the smooth one mirrors it down to -5.
        { "M0 0 Q5 10 10 0", 0, 0, 10, 5 },
        { "M0 0 q5 10 10 0", 0, 0, 10, 5 },
        { "M0 0 Q5 10 10 0 T20 0", 0, -5, 20, 10 },
        { "M0 0 q5 10 10 0 t10 0", 0, -5, 20, 10 },
        // Smooth chains: each one mirrors the one before it. Troughs and peaks alternate at ±7.5 and ±5.
        { "M0 0 C0 10 10 10 10 0 S20 -10 20 0 S30 10 30 0", 0, -7.5, 30, 15 },
        { "M0 0 Q5 10 10 0 T20 0 T30 0", 0, -5, 30, 10 },
        // A smooth curve not directly after its own kind takes the current point as its first control point. The
        // cubic from (20,0) with controls (20,0) and (30,10) peaks at y 4.44, below the first curve's 7.5; a
        // quadratic with its control at the current point is a straight line.
        { "M0 0 C0 10 10 10 10 0 L20 0 S30 10 30 0", 0, 0, 30, 7.5 },
        { "M0 0 c0 10 10 10 10 0 h10 s10 10 10 0", 0, 0, 30, 7.5 },
        { "M0 0 Q5 10 10 0 L20 0 T30 0", 0, 0, 30, 5 },
        { "M0 0 Q5 10 10 0 S20 10 20 0", 0, 0, 20, 5 },
        { "M0 0 C0 10 10 10 10 0 T20 0", 0, 0, 20, 7.5 },
        { "M0 0 C0 10 10 10 10 0 M20 0 S30 10 30 0", 0, 0, 30, 7.5 },
        // The first T is a line to (30,10); the second mirrors the line's start (20,0) to (40,20) and peaks at
        // y 13.33.
        { "M0 0 Q5 10 10 0 L20 0 T30 10 T40 0", 0, 0, 40, 40.0 / 3 },
        // After a line, the second S mirrors the rewritten first one: (30,10) about (30,0) to (30,-10).
        { "M0 0 C0 10 10 10 10 0 L20 0 S30 10 30 0 S40 -10 40 0", 0, -7.5, 40, 15 },
        // The current point is tracked through H and V, z, relative moves and arcs.
        { "M0 0 C0 10 10 10 10 0 H20 V5 S30 15 30 5", 0, 0, 30, 5 + 40.0 / 9 },
        { "M0 0 C0 10 10 10 10 0 Z m20 0 S30 10 30 0", 0, 0, 30, 7.5 },
        { "M0 0 c0 10 10 10 10 0 m10 0 S30 10 30 0", 0, 0, 30, 7.5 },
        { "M0 0 C0 10 10 10 10 0 a5 5 0 0 1 10 0 S30 10 30 0", 0, -5, 30, 12.5 },
        // A negative radius is taken as its absolute value: a half circle of radius 5 over the top.
        { "M0 0 A-5 -5 0 0 1 10 0", 0, -5, 10, 5 },
        // A zero radius is a straight line to the end point.
        { "M0 0 A0 5 0 0 1 10 10", 0, 0, 10, 10 },
        // Radii too small for the end points are scaled up: radius 1 becomes 5, the same half circle.
        { "M0 0 A1 1 0 0 1 10 0", 0, -5, 10, 5 }
    };

    [AvaloniaTheory]
    [MemberData(nameof(PathForms))]
    public void PathData_NormalizedForm_HasSvgBoundsInAvalonia(string d, double x, double y, double width,
        double height)
    {
        Assert.True(SvgPathData.TryNormalize(d, true, out var pathData));

        var bounds = StreamGeometry.Parse(pathData).Bounds;

        Assert.Equal(x, bounds.X, 0.01);
        Assert.Equal(y, bounds.Y, 0.01);
        Assert.Equal(width, bounds.Width, 0.01);
        Assert.Equal(height, bounds.Height, 0.01);
    }
}
