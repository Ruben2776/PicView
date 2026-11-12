using System.Runtime.InteropServices;

namespace PicView.Core.Svg.Parsing;

internal enum SvgPaintKind
{
    None,
    Color,
    CurrentColor
}

/// <summary>
///     A specified paint before opacity: none, a color, or <c>currentColor</c> (resolved at the shape). With a
///     <see cref="Server" /> reference (the text inside <c>url()</c>), the kind and color are the fallback used when the
///     reference does not lead to a paint server.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct SvgPaintValue(SvgPaintKind Kind, SvgColor Color, string? Server = null)
{
    public static SvgPaintValue None { get; } = new(SvgPaintKind.None, default);
}

/// <summary>The computed style of one element: inherited properties come from the parent unless declared.</summary>
internal sealed record SvgComputedStyle
{
    // Inherited.
    public SvgPaintValue Fill { get; init; } = new(SvgPaintKind.Color, SvgColor.Black);
    public SvgPaintValue Stroke { get; init; } = SvgPaintValue.None;
    public SvgColor Color { get; init; } = SvgColor.Black;
    public bool NonZero { get; init; } = true;
    public double FillOpacity { get; init; } = 1;
    public double StrokeOpacity { get; init; } = 1;
    public double StrokeWidth { get; init; } = 1;
    public SvgLineCap LineCap { get; init; } = SvgLineCap.Butt;
    public SvgLineJoin LineJoin { get; init; } = SvgLineJoin.Miter;
    public double MiterLimit { get; init; } = 4;
    public IReadOnlyList<double>? DashArray { get; init; }
    public double DashOffset { get; init; }
    public bool Visible { get; init; } = true;
    public bool ClipNonZero { get; init; } = true;

    // Not inherited.
    public double Opacity { get; init; } = 1;
    public bool Displayed { get; init; } = true;

    /// <summary>The <c>clip-path</c> reference (the text inside <c>url()</c>), or null for none.</summary>
    public string? ClipPath { get; init; }

    public static SvgComputedStyle Initial(SvgParseOptions options)
    {
        return new SvgComputedStyle
        {
            Color = options.CurrentColor,
            Fill = new SvgPaintValue(SvgPaintKind.Color, options.InitialFill)
        };
    }

    /// <summary>The style a child starts from: the inherited properties, with the others at their initial values.</summary>
    public SvgComputedStyle Inherited()
    {
        return Opacity == 1 && Displayed && ClipPath is null
            ? this
            : this with { Opacity = 1, Displayed = true, ClipPath = null };
    }
}

/// <summary>
///     The styles of elements in their place in the document, for content that inherits from where it is defined
///     rather than where it is used: gradient stops and clip paths. Each element's style is computed once.
/// </summary>
internal sealed class SvgContextStyles(SvgViewport viewport, SvgCss css, SvgComputedStyle initial)
{
    private readonly Dictionary<SvgElement, SvgComputedStyle> _styles = [];

    public bool TryGet(SvgElement element, out SvgComputedStyle style, out string? reason)
    {
        reason = null;
        var pending = new Stack<SvgElement>();
        SvgElement? current = element;
        SvgComputedStyle? known = null;
        while (current is not null && !_styles.TryGetValue(current, out known))
        {
            pending.Push(current);
            current = current.Parent;
        }

        style = known ?? initial;
        while (pending.Count > 0)
        {
            var next = pending.Pop();
            if (!SvgStyle.TryComputeElement(next, css, style, viewport, out style, out reason))
            {
                return false;
            }

            _styles[next] = style;
        }

        return true;
    }
}

/// <summary>
///     Declares and computes the style properties. An element's declarations are collected lowest precedence first
///     (presentation attributes, then CSS rules from <c>style</c> elements, then inline <c>style</c>), each later one
///     replacing an earlier one, then computed against the parent's style.
/// </summary>
internal static class SvgStyle
{
    private static readonly string[] None = ["none"];
    private static readonly string[] Normal = ["normal"];
    private static readonly string[] NoBackground = ["none", "transparent"];

    /// <summary>Properties that make the file unsupported unless they have a neutral value (null: never).</summary>
    private static readonly Dictionary<string, string[]?> RestrictedProperties = new(StringComparer.Ordinal)
    {
        ["filter"] = None,
        ["mask"] = None,
        ["marker"] = None,
        ["marker-start"] = None,
        ["marker-mid"] = None,
        ["marker-end"] = None,
        ["vector-effect"] = None,
        ["mix-blend-mode"] = Normal,
        ["paint-order"] = Normal,
        ["transform-origin"] = null,
        ["transform-box"] = null,
        ["transform"] = null,
        // CSS transforms, motion paths and masks a browser applies on top of SVG's own.
        ["rotate"] = null,
        ["scale"] = null,
        ["translate"] = null,
        ["offset-path"] = null,
        ["mask-image"] = null,
        ["mask-mode"] = null,
        ["mask-repeat"] = null,
        ["mask-position"] = null,
        ["mask-clip"] = null,
        ["mask-origin"] = null,
        ["mask-size"] = null,
        ["mask-composite"] = null,
        ["mask-type"] = null,
        ["mask-border"] = null,
        ["-webkit-mask"] = null,
        ["-webkit-mask-image"] = null,
        // all resets every property, inherited ones included.
        ["all"] = null,
        ["clip"] = ["auto"],
        // On the root svg a browser paints the background on the canvas.
        ["background"] = NoBackground,
        ["background-color"] = NoBackground,
        ["background-image"] = NoBackground,
        // SVG 2 geometry properties: as attributes they are geometry, as CSS they are not handled.
        ["width"] = null,
        ["height"] = null,
        ["x"] = null,
        ["y"] = null,
        ["cx"] = null,
        ["cy"] = null,
        ["r"] = null,
        ["rx"] = null,
        ["ry"] = null,
        ["d"] = null
    };

    /// <summary>Attributes read as style properties. <c>transform</c> is not one: as an attribute it is geometry.</summary>
    private static readonly HashSet<string> PresentationAttributes = new(StringComparer.Ordinal)
    {
        "fill", "stroke", "color", "fill-rule", "opacity", "fill-opacity", "stroke-opacity", "stroke-width",
        "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stroke-dasharray", "stroke-dashoffset",
        "display", "visibility", "filter", "mask", "marker", "marker-start", "marker-mid", "marker-end", "clip-path",
        "clip-rule", "stop-color", "stop-opacity", "vector-effect", "mix-blend-mode", "paint-order",
        "transform-origin", "transform-box"
    };

    /// <summary>Declares and computes an element's style against its parent's.</summary>
    public static bool TryComputeElement(SvgElement element, SvgCss css, SvgComputedStyle parent,
        SvgViewport viewport, out SvgComputedStyle style, out string? reason)
    {
        style = parent;
        var declarations = new Dictionary<string, string>(StringComparer.Ordinal);
        return TryDeclare(element, css, declarations, out reason) &&
               TryCompute(parent, declarations, viewport, out style, out reason);
    }

    /// <summary>
    ///     Whether <see cref="TryCompute" /> or the stop code reads the property. Declarations of other properties
    ///     change nothing, so CSS rules keep only these.
    /// </summary>
    public static bool IsStyleProperty(string name)
    {
        return PresentationAttributes.Contains(name) || RestrictedProperties.ContainsKey(name);
    }

    private static readonly HashSet<string> DisplayValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "inline", "block", "inline-block", "list-item", "run-in", "compact", "marker", "table",
        "inline-table", "table-row-group", "table-header-group", "table-footer-group", "table-row",
        "table-column-group", "table-column", "table-cell", "table-caption", "flex", "inline-flex", "grid",
        "inline-grid", "flow-root", "contents"
    };

    /// <summary>Collects an element's declarations, keyed by lower-case property name, values trimmed.</summary>
    public static bool TryDeclare(SvgElement element, SvgCss css, Dictionary<string, string> declarations,
        out string? reason)
    {
        reason = null;
        foreach (var (name, value) in element.Attributes)
        {
            if (PresentationAttributes.Contains(name))
            {
                declarations[name] = value.Trim();
            }
        }

        foreach (var (name, value) in css.Match(element))
        {
            declarations[name] = value;
        }

        return element.GetAttribute("style") is not { } style || TryAddInlineStyle(style, declarations, out reason);
    }

    /// <summary>Adds the declarations of a <c>style=""</c> attribute.</summary>
    public static bool TryAddInlineStyle(string style, Dictionary<string, string> declarations, out string? reason)
    {
        reason = null;
        foreach (var part in style.Split(';'))
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var colon = part.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0 || part.Contains("/*", StringComparison.Ordinal) ||
                part.Contains('!', StringComparison.Ordinal))
            {
                reason = "css";
                return false;
            }

            var name = part[..colon].Trim().ToLowerInvariant();
            var value = part[(colon + 1)..].Trim();
            if (name.Length == 0)
            {
                reason = "css";
                return false;
            }

            declarations[name] = value;
        }

        return true;
    }

    /// <summary>Computes the style from the parent's and the element's declarations.</summary>
    public static bool TryCompute(SvgComputedStyle parent, IReadOnlyDictionary<string, string> declared,
        SvgViewport viewport, out SvgComputedStyle style, out string? reason)
    {
        style = parent.Inherited();
        reason = null;

        foreach (var (name, neutral) in RestrictedProperties)
        {
            if (declared.TryGetValue(name, out var value) && !IsNeutral(value, neutral))
            {
                reason = "property:" + name;
                return false;
            }
        }

        var diagonal = viewport.PercentBasis(SvgAxis.Diagonal);

        if (declared.TryGetValue("color", out var text))
        {
            if (text.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
            {
                // color: currentColor is the inherited color.
            }
            else if (SvgColorParser.TryParse(text, out var color))
            {
                style = style with { Color = color };
            }
            else
            {
                return Fail("color", out reason);
            }
        }

        if (declared.TryGetValue("fill", out text))
        {
            if (!TryParsePaint(text, out var paint))
            {
                return Fail("fill", out reason);
            }

            style = style with { Fill = paint };
        }

        if (declared.TryGetValue("stroke", out text))
        {
            if (!TryParsePaint(text, out var paint))
            {
                return Fail("stroke", out reason);
            }

            style = style with { Stroke = paint };
        }

        if (declared.TryGetValue("clip-path", out text))
        {
            if (text.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { ClipPath = null };
            }
            else if (TryParseUrl(text, out var reference, out var end) && end == text.Length)
            {
                style = style with { ClipPath = reference };
            }
            else
            {
                return Fail("clip-path", out reason);
            }
        }

        if (declared.TryGetValue("clip-rule", out text))
        {
            if (text.Equals("nonzero", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { ClipNonZero = true };
            }
            else if (text.Equals("evenodd", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { ClipNonZero = false };
            }
            else
            {
                return Fail("clip-rule", out reason);
            }
        }

        if (declared.TryGetValue("fill-rule", out text))
        {
            if (text.Equals("nonzero", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { NonZero = true };
            }
            else if (text.Equals("evenodd", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { NonZero = false };
            }
            else
            {
                return Fail("fill-rule", out reason);
            }
        }

        if (declared.TryGetValue("opacity", out text))
        {
            if (!TryParseOpacity(text, out var opacity))
            {
                return Fail("opacity", out reason);
            }

            style = style with { Opacity = opacity };
        }

        if (declared.TryGetValue("fill-opacity", out text))
        {
            if (!TryParseOpacity(text, out var opacity))
            {
                return Fail("fill-opacity", out reason);
            }

            style = style with { FillOpacity = opacity };
        }

        if (declared.TryGetValue("stroke-opacity", out text))
        {
            if (!TryParseOpacity(text, out var opacity))
            {
                return Fail("stroke-opacity", out reason);
            }

            style = style with { StrokeOpacity = opacity };
        }

        if (declared.TryGetValue("stroke-width", out text))
        {
            if (!SvgLength.TryResolve(text, diagonal, out var width) || width < 0)
            {
                return Fail("stroke-width", out reason);
            }

            style = style with { StrokeWidth = width };
        }

        if (declared.TryGetValue("stroke-linecap", out text))
        {
            if (text.Equals("butt", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { LineCap = SvgLineCap.Butt };
            }
            else if (text.Equals("round", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { LineCap = SvgLineCap.Round };
            }
            else if (text.Equals("square", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { LineCap = SvgLineCap.Square };
            }
            else
            {
                return Fail("stroke-linecap", out reason);
            }
        }

        if (declared.TryGetValue("stroke-linejoin", out text))
        {
            if (text.Equals("miter", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { LineJoin = SvgLineJoin.Miter };
            }
            else if (text.Equals("round", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { LineJoin = SvgLineJoin.Round };
            }
            else if (text.Equals("bevel", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { LineJoin = SvgLineJoin.Bevel };
            }
            else
            {
                return Fail("stroke-linejoin", out reason);
            }
        }

        if (declared.TryGetValue("stroke-miterlimit", out text))
        {
            if (!TryParseNumber(text, out var limit) || limit < 1)
            {
                return Fail("stroke-miterlimit", out reason);
            }

            style = style with { MiterLimit = limit };
        }

        if (declared.TryGetValue("stroke-dasharray", out text))
        {
            if (!TryParseDashArray(text, diagonal, out var dashes))
            {
                return Fail("stroke-dasharray", out reason);
            }

            style = style with { DashArray = dashes };
        }

        if (declared.TryGetValue("stroke-dashoffset", out text))
        {
            if (!SvgLength.TryResolve(text, diagonal, out var offset))
            {
                return Fail("stroke-dashoffset", out reason);
            }

            style = style with { DashOffset = offset };
        }

        if (declared.TryGetValue("display", out text))
        {
            if (!DisplayValues.Contains(text))
            {
                return Fail("display", out reason);
            }

            style = style with { Displayed = !text.Equals("none", StringComparison.OrdinalIgnoreCase) };
        }

        if (declared.TryGetValue("visibility", out text))
        {
            if (text.Equals("visible", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { Visible = true };
            }
            else if (text.Equals("hidden", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("collapse", StringComparison.OrdinalIgnoreCase))
            {
                style = style with { Visible = false };
            }
            else
            {
                return Fail("visibility", out reason);
            }
        }

        return true;
    }

    private static bool IsNeutral(string value, string[]? neutral)
    {
        if (neutral is null)
        {
            return false;
        }

        foreach (var candidate in neutral)
        {
            if (value.Equals(candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Fail(string property, out string reason)
    {
        reason = "value:" + property;
        return false;
    }

    /// <summary>
    ///     A paint: <c>none</c>, <c>currentColor</c>, a color, or <c>url(reference)</c> optionally followed by one of
    ///     those as the fallback.
    /// </summary>
    private static bool TryParsePaint(string text, out SvgPaintValue paint)
    {
        paint = SvgPaintValue.None;
        if (!TryParseUrl(text, out var server, out var end))
        {
            return TryParsePlainPaint(text, out paint);
        }

        var fallback = text[end..].Trim();
        if (fallback.Length == 0)
        {
            paint = SvgPaintValue.None with { Server = server };
            return true;
        }

        if (!TryParsePlainPaint(fallback, out var plain))
        {
            return false;
        }

        paint = plain with { Server = server };
        return true;
    }

    /// <summary>
    ///     Reads <c>url(reference)</c> at the start of <paramref name="text" />: the reference without quotes, and the
    ///     position after the closing parenthesis. False when the text does not start with a well-formed url.
    /// </summary>
    public static bool TryParseUrl(string text, out string reference, out int end)
    {
        reference = string.Empty;
        end = 0;
        if (!text.StartsWith("url(", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var close = text.IndexOf(')', 4);
        if (close < 0)
        {
            return false;
        }

        var inner = text.AsSpan(4, close - 4).Trim();
        if (inner.Length >= 2 && (inner[0] == '\'' || inner[0] == '"') && inner[^1] == inner[0])
        {
            inner = inner[1..^1].Trim();
        }

        if (inner.IsEmpty)
        {
            return false;
        }

        reference = inner.ToString();
        end = close + 1;
        return true;
    }

    private static bool TryParsePlainPaint(string text, out SvgPaintValue paint)
    {
        paint = SvgPaintValue.None;
        if (text.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (text.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
        {
            paint = new SvgPaintValue(SvgPaintKind.CurrentColor, default);
            return true;
        }

        if (SvgColorParser.TryParse(text, out var color))
        {
            paint = new SvgPaintValue(SvgPaintKind.Color, color);
            return true;
        }

        return false;
    }

    /// <summary>A number or a percentage, clamped to 0 to 1.</summary>
    public static bool TryParseOpacity(string text, out double opacity)
    {
        opacity = 1;
        var span = text.AsSpan();
        var pos = 0;
        if (!SvgScanner.TryReadNumber(span, ref pos, out var value))
        {
            return false;
        }

        if (pos < span.Length && span[pos] == '%')
        {
            value /= 100;
            pos++;
        }

        if (pos != span.Length)
        {
            return false;
        }

        opacity = Math.Clamp(value, 0, 1);
        return true;
    }

    private static bool TryParseNumber(string text, out double value)
    {
        var pos = 0;
        return SvgScanner.TryReadNumber(text, ref pos, out value) && pos == text.Length;
    }

    /// <summary>
    ///     <c>none</c> or a list of non-negative lengths. An odd-length list is repeated to make it even, and a list of
    ///     zeros means no dashes (null).
    /// </summary>
    private static bool TryParseDashArray(string text, double percentBasis, out IReadOnlyList<double>? dashes)
    {
        dashes = null;
        if (text.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var parts = text.Split([',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var values = new List<double>(parts.Length * 2);
        var total = 0d;
        foreach (var part in parts)
        {
            if (!SvgLength.TryResolve(part, percentBasis, out var length) || length < 0)
            {
                return false;
            }

            values.Add(length);
            total += length;
        }

        if (total == 0)
        {
            return true;
        }

        if (values.Count % 2 == 1)
        {
            values.AddRange(values);
        }

        dashes = values;
        return true;
    }
}
