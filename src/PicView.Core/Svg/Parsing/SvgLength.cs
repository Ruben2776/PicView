using System.Globalization;

namespace PicView.Core.Svg.Parsing;

internal enum SvgLengthUnit
{
    Pixels,
    Percent
}

/// <summary>SVG lengths: a number with an optional unit. A bare number is px; em and rem are 16 px.</summary>
internal static class SvgLength
{
    private const double FontSize = 16;

    /// <summary>
    ///     Parses a length. Absolute units become pixels (<see cref="SvgLengthUnit.Pixels" />); a percentage is returned
    ///     as its number with <see cref="SvgLengthUnit.Percent" />. False for any other syntax. A number too large for a
    ///     double parses as infinity, so callers check <see cref="double.IsFinite" /> and can tell it from bad syntax.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> text, out double value, out SvgLengthUnit unit)
    {
        unit = SvgLengthUnit.Pixels;
        value = 0;
        text = text.Trim();
        var pos = 0;
        if (!SvgScanner.TryScanNumber(text, ref pos, out var number) ||
            !double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
        {
            return false;
        }

        var suffix = text[pos..];
        double scale;
        if (suffix.IsEmpty || suffix.Equals("px", StringComparison.OrdinalIgnoreCase))
        {
            scale = 1;
        }
        else if (suffix is "%")
        {
            unit = SvgLengthUnit.Percent;
            scale = 1;
        }
        else if (suffix.Equals("pt", StringComparison.OrdinalIgnoreCase))
        {
            scale = 96d / 72;
        }
        else if (suffix.Equals("pc", StringComparison.OrdinalIgnoreCase))
        {
            scale = 16;
        }
        else if (suffix.Equals("in", StringComparison.OrdinalIgnoreCase))
        {
            scale = 96;
        }
        else if (suffix.Equals("cm", StringComparison.OrdinalIgnoreCase))
        {
            scale = 96 / 2.54;
        }
        else if (suffix.Equals("mm", StringComparison.OrdinalIgnoreCase))
        {
            scale = 96 / 25.4;
        }
        else if (suffix.Equals("em", StringComparison.OrdinalIgnoreCase) ||
                 suffix.Equals("rem", StringComparison.OrdinalIgnoreCase))
        {
            scale = FontSize;
        }
        else
        {
            return false;
        }

        value = n * scale;
        return true;
    }

    /// <summary>
    ///     Parses a length and resolves a percentage against <paramref name="percentBasis" />. False for bad syntax or
    ///     a non-finite result.
    /// </summary>
    public static bool TryResolve(ReadOnlySpan<char> text, double percentBasis, out double pixels)
    {
        if (!TryParse(text, out var value, out var unit))
        {
            pixels = 0;
            return false;
        }

        pixels = unit == SvgLengthUnit.Percent ? value / 100 * percentBasis : value;
        return double.IsFinite(pixels);
    }
}
