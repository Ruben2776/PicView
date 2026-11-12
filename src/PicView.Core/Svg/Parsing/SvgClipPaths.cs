namespace PicView.Core.Svg.Parsing;

/// <summary>
///     Resolves <c>clip-path</c> references to <c>clipPath</c> elements in user space. Each shape, or <c>use</c> of a
///     shape, becomes a clip shape. The children inherit from the clip path's place in the document, and only their
///     geometry and <c>clip-rule</c> count. Each clip path is resolved once. Shape geometry and <c>use</c>
///     references come from the <see cref="SvgResolver" />, so they read exactly as in the drawing.
/// </summary>
internal sealed class SvgClipPaths(
    SvgDocument document,
    SvgViewport viewport,
    SvgContextStyles contextStyles,
    SvgResolver resolver)
{
    /// <summary>
    ///     A clip path with more shapes than this makes the file unsupported (<c>clip</c>). The union of its shapes is
    ///     a tree of combined geometries, and Avalonia recurses through that tree at render.
    /// </summary>
    public const int MaxShapes = 1_000;

    /// <summary>Resolved clip paths; null where a clip path has no shapes and so clips everything away.</summary>
    private readonly Dictionary<SvgElement, SvgClip?> _clips = [];

    public string? Reason { get; private set; }

    /// <summary>
    ///     The clip a <c>clip-path</c> reference names. Null (with true) when the clip path has no shapes, which
    ///     clips everything away.
    /// </summary>
    public bool TryResolve(string reference, out SvgClip? clip)
    {
        clip = null;
        if (!reference.StartsWith('#'))
        {
            return Fail("reference:" + reference);
        }

        var id = reference[1..];
        if (!document.ById.TryGetValue(id, out var clipPath) ||
            !string.Equals(clipPath.Name, "clipPath", StringComparison.Ordinal))
        {
            return Fail("reference:" + id);
        }

        if (_clips.TryGetValue(clipPath, out clip))
        {
            return true;
        }

        if (!TryBuild(clipPath, out clip))
        {
            return false;
        }

        _clips[clipPath] = clip;
        return true;
    }

    private bool TryBuild(SvgElement clipPath, out SvgClip? clip)
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

        if (!contextStyles.TryGet(clipPath, out var clipStyle, out var reason))
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
            if (!TryAddChild(child, clipStyle, clipTransform, shapes))
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

    private bool TryAddChild(SvgElement child, SvgComputedStyle clipStyle, SvgMatrix? clipTransform,
        List<SvgClipShape> into)
    {
        if (!string.Equals(child.Name, "use", StringComparison.Ordinal))
        {
            return SvgDocument.ShapeElements.Contains(child.Name)
                ? TryAddShape(child, clipStyle, clipTransform, into)
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

        if (!resolver.TryUseTarget(child, out var target, out var id))
        {
            return Fail(resolver.Reason!);
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

        if (!resolver.TryUseTransform(child, transform, out var useTransform))
        {
            return Fail(resolver.Reason!);
        }

        return TryAddShape(target, useStyle, SvgResolver.Combine(useTransform, clipTransform), into);
    }

    /// <summary>Adds a shape's geometry, with its own transform followed by <paramref name="after" />.</summary>
    private bool TryAddShape(SvgElement element, SvgComputedStyle parentStyle, SvgMatrix? after,
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

        if (!resolver.TryShapePathData(element, style.ClipNonZero, out var pathData))
        {
            return Fail(resolver.Reason!);
        }

        if (pathData is null)
        {
            return true;
        }

        into.Add(new SvgClipShape(pathData, SvgResolver.Combine(transform, after)));
        return into.Count <= MaxShapes || Fail("clip");
    }

    private bool TryComputeStyle(SvgElement element, SvgComputedStyle parentStyle, out SvgComputedStyle style)
    {
        return SvgStyle.TryComputeElement(element, document.Css, parentStyle, viewport, out style, out var reason) ||
               Fail(reason!);
    }

    private bool Fail(string reason)
    {
        Reason = reason;
        return false;
    }
}
