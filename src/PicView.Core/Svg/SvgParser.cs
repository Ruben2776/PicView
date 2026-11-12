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

        var resolver = new SvgResolver(document, viewport, options);
        return resolver.TryResolveRoot(out var root)
            ? new SvgParseResult(new SvgDrawing(viewport.Width, viewport.Height, root), null)
            : new SvgParseResult(null, resolver.Reason);
    }
}

/// <summary>
///     Walks the element tree once, computing each element's style from its parent's, turning shapes into path data
///     and expanding <c>use</c>. The walk keeps its own stack of open groups, so expanded content never deepens the
///     call stack; the depth guard (<see cref="SvgDocument.MaxDepth" />) and the expansion guard
///     (<see cref="MaxExpandedNodes" />) bound what it builds.
/// </summary>
internal sealed class SvgResolver
{
    /// <summary>More nodes than this produced by expanding <c>use</c> makes the file unsupported.</summary>
    public const int MaxExpandedNodes = 1_000_000;

    private readonly SvgDocument _document;
    private readonly SvgViewport _viewport;
    private readonly SvgParseOptions _options;
    private readonly SvgContextStyles _contextStyles;
    private readonly SvgPaintServers _paintServers;

    /// <summary>Resolved clip paths; null where a clip path has no shapes and so clips everything away.</summary>
    private readonly Dictionary<SvgElement, SvgClip?> _clips = [];

    /// <summary>The groups being built, innermost on top.</summary>
    private readonly Stack<Frame> _open = new();

    /// <summary>How many open frames each element has, to find <c>use</c> cycles.</summary>
    private readonly Dictionary<SvgElement, int> _onPath = [];

    private int _openUses;
    private int _expandedNodes;

    public SvgResolver(SvgDocument document, SvgViewport viewport, SvgParseOptions options)
    {
        _document = document;
        _viewport = viewport;
        _options = options;
        _contextStyles = new SvgContextStyles(viewport, SvgComputedStyle.Initial(options.CurrentColor));
        _paintServers = new SvgPaintServers(document, viewport, _contextStyles);
    }

    public string? Reason { get; private set; }

    public bool TryResolveRoot(out SvgGroup root)
    {
        var svg = _document.Root;
        root = new SvgGroup(_viewport.Transform, 1, null, []);
        if (svg.GetAttribute("transform") is not null)
        {
            return Fail("property:transform");
        }

        if (!TryComputeStyle(svg, SvgComputedStyle.Initial(_options.CurrentColor), out var style))
        {
            return false;
        }

        if (style.ClipPath is not null)
        {
            return Fail("property:clip-path");
        }

        if (!style.Displayed)
        {
            root = new SvgGroup(_viewport.Transform, style.Opacity, null, []);
            return true;
        }

        Open(new Frame(svg, style, svg.Children, _viewport.Transform, style.Opacity, null, false));
        while (_open.TryPeek(out var frame))
        {
            if (frame.Next < frame.Pending.Count)
            {
                if (!TryVisit(frame.Pending[frame.Next++], frame))
                {
                    return false;
                }

                continue;
            }

            Close(frame);
            var group = new SvgGroup(frame.Transform, frame.Opacity, frame.Clip, frame.Children);
            if (_open.TryPeek(out var parent))
            {
                parent.Children.Add(group);
            }
            else
            {
                root = group;
            }
        }

        return true;
    }

    /// <summary>Adds a shape to <paramref name="parent" />, or opens a frame for a group or <c>use</c>.</summary>
    private bool TryVisit(SvgElement element, Frame parent)
    {
        if (element.Name is "defs" or "linearGradient" or "radialGradient" or "clipPath" or "stop")
        {
            // Definitions are never drawn where they stand.
            return true;
        }

        if (_open.Count >= SvgDocument.MaxDepth)
        {
            return Fail("depth");
        }

        if (element.GetAttribute("pathLength") is not null)
        {
            return Fail("property:pathLength");
        }

        if (!TryComputeStyle(element, parent.Style, out var style))
        {
            return false;
        }

        if (!style.Displayed)
        {
            return true;
        }

        var isShape = SvgDocument.ShapeElements.Contains(element.Name);
        if (isShape && !style.Visible)
        {
            return true;
        }

        if (!SvgTransformParser.TryParse(element.GetAttribute("transform"), out var transform))
        {
            return Fail("value:transform");
        }

        SvgClip? clip = null;
        if (style.ClipPath is { } clipReference)
        {
            if (!TryClip(clipReference, out clip))
            {
                return false;
            }

            if (clip is null)
            {
                // A clip path without shapes clips the element away.
                return true;
            }
        }

        if (isShape)
        {
            return TryAddShape(element, style, transform, clip, parent.Children);
        }

        switch (element.Name)
        {
            case "use":
                return TryOpenUse(element, style, transform, clip);
            case "g":
                Open(new Frame(element, style, element.Children, transform, style.Opacity, clip, false));
                return TryCountExpanded(1);
            default:
                return Fail("element:" + element.Name);
        }
    }

    private bool TryAddShape(SvgElement element, SvgComputedStyle style, SvgMatrix? transform, SvgClip? clip,
        List<SvgNode> into)
    {
        if (!TryShapePathData(element, style.NonZero, out var pathData))
        {
            return false;
        }

        if (pathData is null)
        {
            return true;
        }

        if (!_paintServers.TryResolve(style.Fill, style.FillOpacity, style.Color, out var fill) ||
            !TryStroke(style, out var stroke))
        {
            return Fail(_paintServers.Reason!);
        }

        var shape = new SvgShape(pathData, fill, stroke);
        var wrapped = transform is not null || style.Opacity < 1 || clip is not null;
        if (!TryCountExpanded(wrapped ? 2 : 1))
        {
            return false;
        }

        into.Add(wrapped ? new SvgGroup(transform, style.Opacity, clip, [shape]) : shape);
        return true;
    }

    /// <summary>
    ///     Opens the group of a <c>use</c>: its transform followed by <c>translate(x, y)</c>, holding the referenced
    ///     shape or group, which inherits the <c>use</c>'s style.
    /// </summary>
    private bool TryOpenUse(SvgElement use, SvgComputedStyle style, SvgMatrix? transform, SvgClip? clip)
    {
        if (!TryUseTarget(use, out var target, out var id))
        {
            return false;
        }

        if (target is null)
        {
            return true;
        }

        if ((!SvgDocument.ShapeElements.Contains(target.Name) &&
             !string.Equals(target.Name, "g", StringComparison.Ordinal)) ||
            _onPath.ContainsKey(target))
        {
            return Fail("reference:" + id);
        }

        if (!TryUseTransform(use, transform, out var useTransform))
        {
            return false;
        }

        Open(new Frame(use, style, [target], useTransform, style.Opacity, clip, true));
        return TryCountExpanded(1);
    }

    /// <summary>
    ///     The element a <c>use</c> references by <c>href</c> (or <c>xlink:href</c>), with the id it names. Null (with
    ///     true) when the <c>use</c> has no reference and so draws nothing.
    /// </summary>
    private bool TryUseTarget(SvgElement use, out SvgElement? target, out string id)
    {
        target = null;
        id = string.Empty;
        if (use.GetAttribute("href") is not { } href)
        {
            return true;
        }

        href = href.Trim();
        if (!href.StartsWith('#'))
        {
            id = href;
            return Fail("reference:" + href);
        }

        id = href[1..];
        return _document.ById.TryGetValue(id, out target) || Fail("reference:" + id);
    }

    /// <summary>The <c>use</c>'s transform, applied after its <c>translate(x, y)</c>.</summary>
    private bool TryUseTransform(SvgElement use, SvgMatrix? transform, out SvgMatrix? result)
    {
        result = null;
        if (!TryLength(use, "x", SvgAxis.Horizontal, out var x) || !TryLength(use, "y", SvgAxis.Vertical, out var y))
        {
            return false;
        }

        result = Combine(new SvgMatrix(1, 0, 0, 1, x, y), transform);
        return true;
    }

    /// <summary>The transform that applies <paramref name="first" />, then <paramref name="then" />; null for identity.</summary>
    private static SvgMatrix? Combine(SvgMatrix? first, SvgMatrix? then)
    {
        var combined = (first ?? SvgMatrix.Identity).Then(then ?? SvgMatrix.Identity);
        return combined.IsIdentity ? null : combined;
    }

    /// <summary>
    ///     Counts nodes produced inside a <c>use</c> expansion. A file whose expansion goes past
    ///     <see cref="MaxExpandedNodes" /> is unsupported before anything more is built.
    /// </summary>
    private bool TryCountExpanded(int nodes)
    {
        if (_openUses == 0)
        {
            return true;
        }

        _expandedNodes += nodes;
        return _expandedNodes <= MaxExpandedNodes || Fail("expansion");
    }

    private void Open(Frame frame)
    {
        _open.Push(frame);
        _onPath[frame.Element] = _onPath.GetValueOrDefault(frame.Element) + 1;
        if (frame.IsUse)
        {
            _openUses++;
        }
    }

    private void Close(Frame frame)
    {
        _open.Pop();
        var count = _onPath[frame.Element] - 1;
        if (count == 0)
        {
            _onPath.Remove(frame.Element);
        }
        else
        {
            _onPath[frame.Element] = count;
        }

        if (frame.IsUse)
        {
            _openUses--;
        }
    }

    /// <summary>
    ///     The clip a <c>clip-path</c> reference names. Null (with true) when the clip path has no shapes, which
    ///     clips everything away.
    /// </summary>
    private bool TryClip(string reference, out SvgClip? clip)
    {
        clip = null;
        if (!reference.StartsWith('#'))
        {
            return Fail("reference:" + reference);
        }

        var id = reference[1..];
        if (!_document.ById.TryGetValue(id, out var clipPath) ||
            !string.Equals(clipPath.Name, "clipPath", StringComparison.Ordinal))
        {
            return Fail("reference:" + id);
        }

        if (_clips.TryGetValue(clipPath, out clip))
        {
            return true;
        }

        if (!TryBuildClip(clipPath, out clip))
        {
            return false;
        }

        _clips[clipPath] = clip;
        return true;
    }

    /// <summary>
    ///     A <c>clipPath</c> in user space: each shape, or <c>use</c> of a shape, becomes a clip shape. Its children
    ///     inherit from the clip path's place in the document, and only their geometry and <c>clip-rule</c> count.
    /// </summary>
    private bool TryBuildClip(SvgElement clipPath, out SvgClip? clip)
    {
        clip = null;
        switch (clipPath.GetAttribute("clipPathUnits"))
        {
            case null or "userSpaceOnUse":
                break;
            case "objectBoundingBox":
                return Fail("property:clipPathUnits");
            default:
                return Fail("value:clipPathUnits");
        }

        if (!_contextStyles.TryGet(clipPath, out var clipStyle, out var reason))
        {
            return Fail(reason!);
        }

        if (clipStyle.ClipPath is not null)
        {
            return Fail("element:clipPath");
        }

        if (!SvgTransformParser.TryParse(clipPath.GetAttribute("transform"), out var clipTransform))
        {
            return Fail("value:transform");
        }

        var shapes = new List<SvgClipShape>();
        foreach (var child in clipPath.Children)
        {
            if (!TryAddClipChild(child, clipStyle, clipTransform, shapes))
            {
                return false;
            }
        }

        if (shapes.Count > 0)
        {
            clip = new SvgClip(shapes);
        }

        return true;
    }

    private bool TryAddClipChild(SvgElement child, SvgComputedStyle clipStyle, SvgMatrix? clipTransform,
        List<SvgClipShape> into)
    {
        if (!string.Equals(child.Name, "use", StringComparison.Ordinal))
        {
            return SvgDocument.ShapeElements.Contains(child.Name)
                ? TryAddClipShape(child, clipStyle, clipTransform, into)
                : Fail("element:clipPath");
        }

        if (!TryComputeStyle(child, clipStyle, out var useStyle))
        {
            return false;
        }

        if (useStyle.ClipPath is not null)
        {
            return Fail("element:clipPath");
        }

        if (!useStyle.Displayed)
        {
            return true;
        }

        if (!TryUseTarget(child, out var target, out var id))
        {
            return false;
        }

        if (target is null)
        {
            return true;
        }

        if (string.Equals(target.Name, "g", StringComparison.Ordinal))
        {
            return Fail("element:clipPath");
        }

        if (!SvgDocument.ShapeElements.Contains(target.Name))
        {
            return Fail("reference:" + id);
        }

        if (!SvgTransformParser.TryParse(child.GetAttribute("transform"), out var transform))
        {
            return Fail("value:transform");
        }

        return TryUseTransform(child, transform, out var useTransform) &&
               TryAddClipShape(target, useStyle, Combine(useTransform, clipTransform), into);
    }

    /// <summary>Adds a shape's geometry, with its own transform followed by <paramref name="after" />.</summary>
    private bool TryAddClipShape(SvgElement element, SvgComputedStyle parentStyle, SvgMatrix? after,
        List<SvgClipShape> into)
    {
        if (element.GetAttribute("pathLength") is not null)
        {
            return Fail("property:pathLength");
        }

        if (!TryComputeStyle(element, parentStyle, out var style))
        {
            return false;
        }

        if (style.ClipPath is not null)
        {
            return Fail("element:clipPath");
        }

        if (!style.Displayed || !style.Visible)
        {
            return true;
        }

        if (!SvgTransformParser.TryParse(element.GetAttribute("transform"), out var transform))
        {
            return Fail("value:transform");
        }

        if (!TryShapePathData(element, style.ClipNonZero, out var pathData))
        {
            return false;
        }

        if (pathData is not null)
        {
            into.Add(new SvgClipShape(pathData, Combine(transform, after)));
        }

        return true;
    }

    private bool TryComputeStyle(SvgElement element, SvgComputedStyle parentStyle, out SvgComputedStyle style)
    {
        return SvgStyle.TryComputeElement(element, parentStyle, _viewport, out style, out var reason) ||
               Fail(reason!);
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

        return SvgLength.TryResolve(text, _viewport.PercentBasis(axis), out value) || Fail("value:" + name);
    }

    /// <summary>A length attribute that may not be negative, 0 when absent.</summary>
    private bool TrySize(SvgElement element, string name, SvgAxis axis, out double value)
    {
        return TryLength(element, name, axis, out value) && (value >= 0 || Fail("value:" + name));
    }

    /// <summary>The stroke, or null (with true) when it has no paint or no width.</summary>
    private bool TryStroke(SvgComputedStyle style, out SvgStroke? stroke)
    {
        stroke = null;
        if (!_paintServers.TryResolve(style.Stroke, style.StrokeOpacity, style.Color, out var paint))
        {
            return false;
        }

        if (paint is not null && style.StrokeWidth > 0)
        {
            stroke = new SvgStroke(paint, style.StrokeWidth, style.LineCap, style.LineJoin, style.MiterLimit,
                style.DashArray, style.DashOffset);
        }

        return true;
    }

    private bool Fail(string reason)
    {
        Reason = reason;
        return false;
    }

    /// <summary>
    ///     A group being built: the element it comes from, the style its children inherit, the elements still to
    ///     visit, and the nodes built so far.
    /// </summary>
    private sealed class Frame(
        SvgElement element,
        SvgComputedStyle style,
        IReadOnlyList<SvgElement> pending,
        SvgMatrix? transform,
        double opacity,
        SvgClip? clip,
        bool isUse)
    {
        public SvgElement Element { get; } = element;
        public SvgComputedStyle Style { get; } = style;
        public IReadOnlyList<SvgElement> Pending { get; } = pending;
        public SvgMatrix? Transform { get; } = transform;
        public double Opacity { get; } = opacity;
        public SvgClip? Clip { get; } = clip;
        public bool IsUse { get; } = isUse;
        public List<SvgNode> Children { get; } = [];
        public int Next { get; set; }
    }
}
