using PicView.Core.Svg.Parsing;

namespace PicView.Core.Svg;

public sealed record SvgParseOptions
{
    public static SvgParseOptions Default { get; } = new();

    /// <summary>The color <c>currentColor</c> means where no <c>color</c> property sets it.</summary>
    public SvgColor CurrentColor { get; init; } = SvgColor.Black;
}

/// <summary>A parsed drawing, or the reason the file is unsupported (and no drawing).</summary>
public readonly record struct SvgParseResult(SvgDrawing? Drawing, string? UnsupportedReason);

/// <summary>
///     Parses SVG text into an <see cref="SvgDrawing" />. Works from an allow-list: anything it does not handle makes
///     the whole file unsupported, with a reason. Never throws for content.
/// </summary>
public static class SvgParser
{
    public static SvgParseResult Parse(string svgText, SvgParseOptions? options = null)
    {
        options ??= SvgParseOptions.Default;
        if (!SvgDocument.TryRead(svgText, out var document, out var reason) ||
            !SvgViewport.TryResolve(document!.Root, out var viewport, out reason))
        {
            return new SvgParseResult(null, reason);
        }

        var resolver = new SvgResolver(viewport, options);
        return resolver.TryResolveRoot(document.Root, out var root)
            ? new SvgParseResult(new SvgDrawing(viewport.Width, viewport.Height, root), null)
            : new SvgParseResult(null, resolver.Reason);
    }
}

/// <summary>
///     Walks the element tree once, computing each element's style from its parent's and turning shapes into path
///     data. Recursion is bounded by <see cref="SvgDocument.MaxDepth" />, which the reader enforces.
/// </summary>
internal sealed class SvgResolver(SvgViewport viewport, SvgParseOptions options)
{
    public string? Reason { get; private set; }

    public bool TryResolveRoot(SvgElement svg, out SvgGroup root)
    {
        root = new SvgGroup(viewport.Transform, 1, null, []);
        if (svg.GetAttribute("transform") is not null)
        {
            return Fail("property:transform");
        }

        if (!TryComputeStyle(svg, SvgComputedStyle.Initial(options.CurrentColor), out var style))
        {
            return false;
        }

        var children = new List<SvgNode>();
        if (style.Displayed && !TryWalkChildren(svg, style, children))
        {
            return false;
        }

        root = new SvgGroup(viewport.Transform, style.Opacity, null, children);
        return true;
    }

    private bool TryWalkChildren(SvgElement parent, SvgComputedStyle parentStyle, List<SvgNode> into)
    {
        foreach (var child in parent.Children)
        {
            if (!TryWalk(child, parentStyle, into))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryWalk(SvgElement element, SvgComputedStyle parentStyle, List<SvgNode> into)
    {
        if (element.GetAttribute("pathLength") is not null)
        {
            return Fail("property:pathLength");
        }

        if (!TryComputeStyle(element, parentStyle, out var style))
        {
            return false;
        }

        if (!style.Displayed)
        {
            return true;
        }

        if (!SvgTransformParser.TryParse(element.GetAttribute("transform"), out var transform))
        {
            return Fail("value:transform");
        }

        if (string.Equals(element.Name, "g", StringComparison.Ordinal))
        {
            var children = new List<SvgNode>();
            if (!TryWalkChildren(element, style, children))
            {
                return false;
            }

            into.Add(new SvgGroup(transform, style.Opacity, null, children));
            return true;
        }

        if (!style.Visible)
        {
            return true;
        }

        if (!TryShapePathData(element, style.NonZero, out var pathData))
        {
            return false;
        }

        if (pathData is null)
        {
            return true;
        }

        var shape = new SvgShape(pathData, Paint(style.Fill, style.FillOpacity, style), Stroke(style));
        into.Add(transform is not null || style.Opacity < 1
            ? new SvgGroup(transform, style.Opacity, null, [shape])
            : shape);
        return true;
    }

    private bool TryComputeStyle(SvgElement element, SvgComputedStyle parentStyle, out SvgComputedStyle style)
    {
        style = parentStyle;
        var declarations = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!SvgStyle.TryDeclare(element, declarations, out var reason) ||
            !SvgStyle.TryCompute(parentStyle, declarations, viewport, out style, out reason))
        {
            return Fail(reason!);
        }

        return true;
    }

    /// <summary>The shape's path data; null (with true) for a shape that draws nothing and is skipped.</summary>
    private bool TryShapePathData(SvgElement element, bool nonZero, out string? pathData)
    {
        pathData = null;
        switch (element.Name)
        {
            case "path":
            {
                var d = element.GetAttribute("d");
                if (string.IsNullOrWhiteSpace(d))
                {
                    return true;
                }

                if (!SvgPathData.TryNormalize(d, nonZero, out var normalized))
                {
                    return Fail("value:d");
                }

                pathData = normalized;
                return true;
            }
            case "rect":
            {
                if (!TryLength(element, "x", SvgAxis.Horizontal, out var x) ||
                    !TryLength(element, "y", SvgAxis.Vertical, out var y) ||
                    !TrySize(element, "width", SvgAxis.Horizontal, out var width) ||
                    !TrySize(element, "height", SvgAxis.Vertical, out var height) ||
                    !TrySize(element, "rx", SvgAxis.Horizontal, out var rx) ||
                    !TrySize(element, "ry", SvgAxis.Vertical, out var ry))
                {
                    return false;
                }

                if (width == 0 || height == 0)
                {
                    return true;
                }

                if (!double.IsFinite(x + width))
                {
                    return Fail("value:width");
                }

                if (!double.IsFinite(y + height))
                {
                    return Fail("value:height");
                }

                var hasRx = element.GetAttribute("rx") is not null;
                var hasRy = element.GetAttribute("ry") is not null;
                if (hasRx && !hasRy)
                {
                    ry = rx;
                }
                else if (hasRy && !hasRx)
                {
                    rx = ry;
                }

                pathData = SvgPathData.Rect(x, y, width, height, rx, ry, nonZero);
                return true;
            }
            case "circle":
            {
                if (!TryLength(element, "cx", SvgAxis.Horizontal, out var cx) ||
                    !TryLength(element, "cy", SvgAxis.Vertical, out var cy) ||
                    !TrySize(element, "r", SvgAxis.Diagonal, out var r))
                {
                    return false;
                }

                if (!double.IsFinite(cx - r) || !double.IsFinite(cx + r))
                {
                    return Fail("value:r");
                }

                if (r > 0)
                {
                    pathData = SvgPathData.Circle(cx, cy, r, nonZero);
                }

                return true;
            }
            case "ellipse":
            {
                if (!TryLength(element, "cx", SvgAxis.Horizontal, out var cx) ||
                    !TryLength(element, "cy", SvgAxis.Vertical, out var cy) ||
                    !TrySize(element, "rx", SvgAxis.Horizontal, out var rx) ||
                    !TrySize(element, "ry", SvgAxis.Vertical, out var ry))
                {
                    return false;
                }

                if (!double.IsFinite(cx - rx) || !double.IsFinite(cx + rx))
                {
                    return Fail("value:rx");
                }

                if (rx > 0 && ry > 0)
                {
                    pathData = SvgPathData.Ellipse(cx, cy, rx, ry, nonZero);
                }

                return true;
            }
            case "line":
            {
                if (!TryLength(element, "x1", SvgAxis.Horizontal, out var x1) ||
                    !TryLength(element, "y1", SvgAxis.Vertical, out var y1) ||
                    !TryLength(element, "x2", SvgAxis.Horizontal, out var x2) ||
                    !TryLength(element, "y2", SvgAxis.Vertical, out var y2))
                {
                    return false;
                }

                pathData = SvgPathData.Line(x1, y1, x2, y2, nonZero);
                return true;
            }
            case "polyline":
            case "polygon":
            {
                var points = new List<double>();
                if (!SvgScanner.TryReadNumberList(element.GetAttribute("points") ?? string.Empty, points) ||
                    points.Count % 2 == 1)
                {
                    return Fail("value:points");
                }

                if (points.Count >= 4)
                {
                    pathData = string.Equals(element.Name, "polygon", StringComparison.Ordinal)
                        ? SvgPathData.Polygon(points, nonZero)
                        : SvgPathData.Polyline(points, nonZero);
                }

                return true;
            }
            default:
                return Fail("element:" + element.Name);
        }
    }

    /// <summary>A length attribute, 0 when absent.</summary>
    private bool TryLength(SvgElement element, string name, SvgAxis axis, out double value)
    {
        value = 0;
        var text = element.GetAttribute(name);
        if (text is null)
        {
            return true;
        }

        return SvgLength.TryResolve(text, viewport.PercentBasis(axis), out value) || Fail("value:" + name);
    }

    /// <summary>A length attribute that may not be negative, 0 when absent.</summary>
    private bool TrySize(SvgElement element, string name, SvgAxis axis, out double value)
    {
        return TryLength(element, name, axis, out value) && (value >= 0 || Fail("value:" + name));
    }

    private SvgStroke? Stroke(SvgComputedStyle style)
    {
        var paint = Paint(style.Stroke, style.StrokeOpacity, style);
        if (paint is null || style.StrokeWidth <= 0)
        {
            return null;
        }

        return new SvgStroke(paint, style.StrokeWidth, style.LineCap, style.LineJoin, style.MiterLimit,
            style.DashArray, style.DashOffset);
    }

    /// <summary>Resolves <c>currentColor</c> and folds the paint's opacity into the color's alpha.</summary>
    private static SvgPaint? Paint(SvgPaintValue paint, double opacity, SvgComputedStyle style)
    {
        var color = paint.Kind switch
        {
            SvgPaintKind.Color => paint.Color,
            SvgPaintKind.CurrentColor => style.Color,
            _ => (SvgColor?)null
        };
        if (color is not { } c)
        {
            return null;
        }

        var alpha = (byte)Math.Round(c.A * opacity, MidpointRounding.AwayFromZero);
        return new SvgSolidPaint(c with { A = alpha });
    }

    private bool Fail(string reason)
    {
        Reason = reason;
        return false;
    }
}
