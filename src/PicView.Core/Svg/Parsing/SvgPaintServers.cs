namespace PicView.Core.Svg.Parsing;

/// <summary>
///     Resolves paints: solid colors, and <c>url()</c> references to <c>linearGradient</c> and
///     <c>radialGradient</c>, with their <c>href</c> inheritance. Each gradient element is resolved once.
/// </summary>
internal sealed class SvgPaintServers(SvgDocument document, SvgViewport viewport, SvgContextStyles contextStyles)
{
    /// <summary>Resolved gradients at full opacity; null where the gradient paints nothing.</summary>
    private readonly Dictionary<SvgElement, SvgPaint?> _gradients = [];

    public string? Reason { get; private set; }

    /// <summary>
    ///     The paint for a shape: <paramref name="opacity" /> is the fill or stroke opacity, and
    ///     <paramref name="currentColor" /> the shape's <c>color</c>. A reference that does not lead to a gradient
    ///     uses the fallback, as SVG says. Null (with true) for no paint.
    /// </summary>
    public bool TryResolve(SvgPaintValue paint, double opacity, SvgColor currentColor, out SvgPaint? result)
    {
        result = null;
        if (paint.Server is { } reference)
        {
            if (!reference.StartsWith('#'))
            {
                return Fail("reference:" + reference);
            }

            if (document.ById.TryGetValue(reference[1..], out var server) && IsGradient(server))
            {
                if (!TryGradient(server, out var gradient))
                {
                    return false;
                }

                result = WithOpacity(gradient, opacity);
                return true;
            }
        }

        var color = paint.Kind switch
        {
            SvgPaintKind.Color => paint.Color,
            SvgPaintKind.CurrentColor => currentColor,
            _ => (SvgColor?)null
        };
        if (color is { } c)
        {
            result = Solid(c, opacity);
        }

        return true;
    }

    private static bool IsGradient(SvgElement element)
    {
        return element.Name is "linearGradient" or "radialGradient";
    }

    private static SvgPaint? WithOpacity(SvgPaint? paint, double opacity)
    {
        return paint switch
        {
            SvgSolidPaint solid => Solid(solid.Color, opacity),
            SvgGradientPaint gradient => gradient with { Opacity = opacity },
            _ => null
        };
    }

    private static SvgSolidPaint Solid(SvgColor color, double opacity)
    {
        var alpha = (byte)Math.Round(color.A * opacity, MidpointRounding.AwayFromZero);
        return new SvgSolidPaint(color with { A = alpha });
    }

    private bool TryGradient(SvgElement element, out SvgPaint? paint)
    {
        if (_gradients.TryGetValue(element, out paint))
        {
            return true;
        }

        if (!TryBuildGradient(element, out paint))
        {
            return false;
        }

        _gradients[element] = paint;
        return true;
    }

    private bool TryBuildGradient(SvgElement element, out SvgPaint? paint)
    {
        paint = null;
        if (!TryChain(element, out var chain))
        {
            return false;
        }

        var gradient = new GradientAttributes(element, chain);
        var stops = new List<SvgGradientStop>();
        if (gradient.StopsElement is { } stopsElement && !TryStops(stopsElement, stops))
        {
            return false;
        }

        SvgGradientUnits units;
        switch (gradient.Get("gradientUnits"))
        {
            case null or "objectBoundingBox":
                units = SvgGradientUnits.ObjectBoundingBox;
                break;
            case "userSpaceOnUse":
                units = SvgGradientUnits.UserSpaceOnUse;
                break;
            default:
                return Fail("value:gradientUnits");
        }

        SvgSpreadMethod spread;
        switch (gradient.Get("spreadMethod"))
        {
            case null or "pad":
                spread = SvgSpreadMethod.Pad;
                break;
            case "reflect":
                spread = SvgSpreadMethod.Reflect;
                break;
            case "repeat":
                spread = SvgSpreadMethod.Repeat;
                break;
            default:
                return Fail("value:spreadMethod");
        }

        if (!SvgTransformParser.TryParse(gradient.Get("gradientTransform"), out var transform))
        {
            return Fail("value:gradientTransform");
        }

        if (string.Equals(element.Name, "linearGradient", StringComparison.Ordinal))
        {
            if (!TryCoordinate(gradient, "x1", "0%", SvgAxis.Horizontal, units, out var x1) ||
                !TryCoordinate(gradient, "y1", "0%", SvgAxis.Vertical, units, out var y1) ||
                !TryCoordinate(gradient, "x2", "100%", SvgAxis.Horizontal, units, out var x2) ||
                !TryCoordinate(gradient, "y2", "0%", SvgAxis.Vertical, units, out var y2))
            {
                return false;
            }

            paint = stops.Count switch
            {
                0 => null,
                1 => Solid(stops[0].Color, 1),
                // Coincident points paint the last stop's color (SVG).
                _ when x1 == x2 && y1 == y2 => Solid(stops[^1].Color, 1),
                _ => new SvgLinearGradientPaint(x1, y1, x2, y2, stops, units, transform, spread, 1)
            };
            return true;
        }

        // fx and fy default to cx and cy.
        var cxText = gradient.GetOwn("cx") ?? "50%";
        var cyText = gradient.GetOwn("cy") ?? "50%";
        if (!TryCoordinate(gradient, "cx", cxText, SvgAxis.Horizontal, units, out var cx) ||
            !TryCoordinate(gradient, "cy", cyText, SvgAxis.Vertical, units, out var cy) ||
            !TryCoordinate(gradient, "r", "50%", SvgAxis.Diagonal, units, out var r) ||
            !TryCoordinate(gradient, "fx", cxText, SvgAxis.Horizontal, units, out var fx) ||
            !TryCoordinate(gradient, "fy", cyText, SvgAxis.Vertical, units, out var fy) ||
            !TryCoordinate(gradient, "fr", "0", SvgAxis.Diagonal, units, out var fr))
        {
            return false;
        }

        if (r < 0)
        {
            return Fail("value:r");
        }

        if (fr != 0)
        {
            return Fail("property:fr");
        }

        paint = stops.Count switch
        {
            0 => null,
            1 => Solid(stops[0].Color, 1),
            // A zero radius paints the last stop's color (SVG).
            _ when r == 0 => Solid(stops[^1].Color, 1),
            _ => new SvgRadialGradientPaint(cx, cy, r, fx, fy, stops, units, transform, spread, 1)
        };
        return true;
    }

    /// <summary>The gradient and the gradients its <c>href</c> chain leads to, nearest first.</summary>
    private bool TryChain(SvgElement element, out List<SvgElement> chain)
    {
        chain = [element];
        var seen = new HashSet<SvgElement> { element };
        var current = element;
        while (current.GetAttribute("href") is { } href)
        {
            href = href.Trim();
            if (!href.StartsWith('#'))
            {
                return Fail("reference:" + href);
            }

            var id = href[1..];
            if (!document.ById.TryGetValue(id, out var next) || !IsGradient(next) || !seen.Add(next))
            {
                return Fail("reference:" + id);
            }

            chain.Add(next);
            current = next;
        }

        return true;
    }

    /// <summary>
    ///     A gradient coordinate, <paramref name="fallback" /> when no gradient in the chain sets it. With
    ///     <c>objectBoundingBox</c> a number or percentage is a fraction; with <c>userSpaceOnUse</c> it is a length,
    ///     percentages of the viewport.
    /// </summary>
    private bool TryCoordinate(GradientAttributes gradient, string name, string fallback, SvgAxis axis,
        SvgGradientUnits units, out double value)
    {
        var text = gradient.GetOwn(name) ?? fallback;
        var valid = units == SvgGradientUnits.ObjectBoundingBox
            ? TryFraction(text, out value)
            : SvgLength.TryResolve(text, viewport.PercentBasis(axis), out value);
        return valid || Fail("value:" + name);
    }

    /// <summary>A number, or a percentage as a fraction.</summary>
    private static bool TryFraction(string text, out double value)
    {
        var span = text.AsSpan().Trim();
        var pos = 0;
        if (!SvgScanner.TryReadNumber(span, ref pos, out value))
        {
            return false;
        }

        if (pos < span.Length && span[pos] == '%')
        {
            value /= 100;
            pos++;
        }

        return pos == span.Length && double.IsFinite(value);
    }

    /// <summary>
    ///     The <c>stop</c> children: offsets clamped to 0..1 and raised to the previous offset, colors with
    ///     <c>stop-opacity</c> folded in, <c>currentColor</c> taken from the stop's place in the document.
    /// </summary>
    private bool TryStops(SvgElement gradient, List<SvgGradientStop> stops)
    {
        if (!contextStyles.TryGet(gradient, out var gradientStyle, out var reason))
        {
            return Fail(reason!);
        }

        var previous = 0d;
        foreach (var stop in gradient.Children)
        {
            if (!string.Equals(stop.Name, "stop", StringComparison.Ordinal))
            {
                continue;
            }

            var offset = 0d;
            if (stop.GetAttribute("offset") is { } offsetText && !TryFraction(offsetText, out offset))
            {
                return Fail("value:offset");
            }

            offset = Math.Max(Math.Clamp(offset, 0, 1), previous);
            previous = offset;

            // One set of declarations gives the stop's style (for currentColor) and its stop properties.
            var declarations = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!SvgStyle.TryDeclare(stop, document.Css, declarations, out reason) ||
                !SvgStyle.TryCompute(gradientStyle, declarations, viewport, out var style, out reason))
            {
                return Fail(reason!);
            }

            var color = SvgColor.Black;
            if (declarations.TryGetValue("stop-color", out var text))
            {
                if (text.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
                {
                    color = style.Color;
                }
                else if (!SvgColorParser.TryParse(text, out color))
                {
                    return Fail("value:stop-color");
                }
            }

            var opacity = 1d;
            if (declarations.TryGetValue("stop-opacity", out text) && !SvgStyle.TryParseOpacity(text, out opacity))
            {
                return Fail("value:stop-opacity");
            }

            stops.Add(new SvgGradientStop(offset, Solid(color, opacity).Color));
        }

        return true;
    }

    private bool Fail(string reason)
    {
        Reason = reason;
        return false;
    }

    /// <summary>
    ///     Attribute lookup through an <c>href</c> chain: the nearest gradient that sets an attribute wins. Attributes
    ///     of one gradient type come only from gradients of that type.
    /// </summary>
    private readonly struct GradientAttributes(SvgElement element, List<SvgElement> chain)
    {
        /// <summary>The nearest gradient with <c>stop</c> children, if any.</summary>
        public SvgElement? StopsElement =>
            chain.Find(e => e.Children.Any(c => string.Equals(c.Name, "stop", StringComparison.Ordinal)));

        /// <summary>An attribute common to both gradient types.</summary>
        public string? Get(string name)
        {
            foreach (var link in chain)
            {
                if (link.GetAttribute(name) is { } value)
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>An attribute of this gradient's type.</summary>
        public string? GetOwn(string name)
        {
            foreach (var link in chain)
            {
                if (string.Equals(link.Name, element.Name, StringComparison.Ordinal) &&
                    link.GetAttribute(name) is { } value)
                {
                    return value;
                }
            }

            return null;
        }
    }
}
