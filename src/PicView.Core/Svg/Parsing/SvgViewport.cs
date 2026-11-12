using System.Runtime.InteropServices;

namespace PicView.Core.Svg.Parsing;

/// <summary>Which viewport dimension a percentage length is measured against.</summary>
internal enum SvgAxis
{
    Horizontal,
    Vertical,
    Diagonal
}

/// <summary>
///     The root viewport: the drawing's size, the transform that maps the viewBox onto it, and the basis for
///     percentage lengths (the viewBox size when there is one, else the width and height).
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct SvgViewport(
    double Width,
    double Height,
    SvgMatrix? Transform,
    double PercentWidth,
    double PercentHeight)
{
    public double PercentBasis(SvgAxis axis)
    {
        return axis switch
        {
            SvgAxis.Horizontal => PercentWidth,
            SvgAxis.Vertical => PercentHeight,
            _ => Math.Sqrt((PercentWidth * PercentWidth + PercentHeight * PercentHeight) / 2)
        };
    }

    /// <summary>Resolves the root <c>svg</c> element's viewport, or gives the reason it has no usable one.</summary>
    public static bool TryResolve(SvgElement svg, out SvgViewport viewport, out string? reason)
    {
        viewport = default;
        reason = null;

        double? viewBoxX = null, viewBoxY = null, viewBoxWidth = null, viewBoxHeight = null;
        var viewBoxText = svg.GetAttribute("viewBox");
        if (viewBoxText is not null)
        {
            var numbers = new List<double>(4);
            if (!SvgScanner.TryReadNumberList(viewBoxText, numbers) || numbers.Count != 4)
            {
                reason = "value:viewBox";
                return false;
            }

            if (numbers[2] <= 0 || numbers[3] <= 0)
            {
                reason = "size";
                return false;
            }

            viewBoxX = numbers[0];
            viewBoxY = numbers[1];
            viewBoxWidth = numbers[2];
            viewBoxHeight = numbers[3];
        }

        if (!TryReadDimension(svg, "width", out var width, out reason) ||
            !TryReadDimension(svg, "height", out var height, out reason))
        {
            return false;
        }

        if (viewBoxWidth is { } vbWidth && viewBoxHeight is { } vbHeight)
        {
            if (width is null && height is null)
            {
                width = vbWidth;
                height = vbHeight;
            }
            else if (width is null)
            {
                width = height * vbWidth / vbHeight;
            }
            else if (height is null)
            {
                height = width * vbHeight / vbWidth;
            }
        }

        if (width is not { } w || height is not { } h || !IsUsable(w) || !IsUsable(h))
        {
            reason = "size";
            return false;
        }

        if (viewBoxWidth is null)
        {
            viewport = new SvgViewport(w, h, null, w, h);
            return true;
        }

        if (!TryViewBoxTransform(svg.GetAttribute("preserveAspectRatio"), viewBoxX!.Value, viewBoxY!.Value,
                viewBoxWidth.Value, viewBoxHeight!.Value, w, h, out var transform))
        {
            reason = "value:preserveAspectRatio";
            return false;
        }

        viewport = new SvgViewport(w, h, transform.IsIdentity ? null : transform, viewBoxWidth.Value,
            viewBoxHeight.Value);
        return true;
    }

    private static bool IsUsable(double value)
    {
        return double.IsFinite(value) && value > 0;
    }

    /// <summary>A missing or percentage dimension is null; it is then derived from the viewBox.</summary>
    private static bool TryReadDimension(SvgElement svg, string name, out double? value, out string? reason)
    {
        value = null;
        reason = null;
        var text = svg.GetAttribute(name);
        if (text is null)
        {
            return true;
        }

        if (!SvgLength.TryParse(text, out var number, out var unit))
        {
            reason = "value:" + name;
            return false;
        }

        if (unit == SvgLengthUnit.Pixels)
        {
            value = number;
        }

        return true;
    }

    private static bool TryViewBoxTransform(string? preserveAspectRatio, double x, double y, double viewBoxWidth,
        double viewBoxHeight, double width, double height, out SvgMatrix transform)
    {
        transform = SvgMatrix.Identity;
        if (!TryParsePreserveAspectRatio(preserveAspectRatio, out var alignX, out var alignY, out var slice))
        {
            return false;
        }

        var scaleX = width / viewBoxWidth;
        var scaleY = height / viewBoxHeight;
        if (alignX is null || alignY is null)
        {
            transform = new SvgMatrix(scaleX, 0, 0, scaleY, -x * scaleX, -y * scaleY);
            return true;
        }

        var scale = slice ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);
        var offsetX = -x * scale + alignX.Value * (width - viewBoxWidth * scale);
        var offsetY = -y * scale + alignY.Value * (height - viewBoxHeight * scale);
        transform = new SvgMatrix(scale, 0, 0, scale, offsetX, offsetY);
        return true;
    }

    /// <summary>
    ///     Reads <c>[defer] &lt;align&gt; [meet | slice]</c>. The align fractions are null for <c>none</c>. The default
    ///     is <c>xMidYMid meet</c>.
    /// </summary>
    private static bool TryParsePreserveAspectRatio(string? text, out double? alignX, out double? alignY,
        out bool slice)
    {
        alignX = 0.5;
        alignY = 0.5;
        slice = false;
        if (text is null)
        {
            return true;
        }

        var parts = text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var index = 0;
        if (index < parts.Length && parts[index].Equals("defer", StringComparison.OrdinalIgnoreCase))
        {
            index++;
        }

        if (index >= parts.Length)
        {
            return false;
        }

        var align = parts[index++];
        if (align.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            alignX = null;
            alignY = null;
        }
        else if (align.Length == 8 &&
                 TryAlign(align.AsSpan(0, 4), 'x', out var x) &&
                 TryAlign(align.AsSpan(4, 4), 'y', out var y))
        {
            alignX = x;
            alignY = y;
        }
        else
        {
            return false;
        }

        if (index < parts.Length)
        {
            if (parts[index].Equals("slice", StringComparison.OrdinalIgnoreCase))
            {
                slice = true;
            }
            else if (!parts[index].Equals("meet", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            index++;
        }

        return index == parts.Length;
    }

    private static bool TryAlign(ReadOnlySpan<char> part, char axis, out double fraction)
    {
        fraction = 0;
        if (char.ToLowerInvariant(part[0]) != axis)
        {
            return false;
        }

        var position = part[1..];
        if (position.Equals("Min", StringComparison.OrdinalIgnoreCase))
        {
            fraction = 0;
        }
        else if (position.Equals("Mid", StringComparison.OrdinalIgnoreCase))
        {
            fraction = 0.5;
        }
        else if (position.Equals("Max", StringComparison.OrdinalIgnoreCase))
        {
            fraction = 1;
        }
        else
        {
            return false;
        }

        return true;
    }
}
