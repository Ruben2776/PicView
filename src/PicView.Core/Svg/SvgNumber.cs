using System.Globalization;

namespace PicView.Core.Svg;

public static class SvgNumber
{
    /// <summary>Formats with the invariant culture and up to three decimals; negative zero prints as "0".</summary>
    public static string Format(double value)
    {
        var text = value.ToString("0.###", CultureInfo.InvariantCulture);
        return string.Equals(text, "-0", StringComparison.Ordinal) ? "0" : text;
    }

    /// <summary>Parses an invariant-form number (sign, decimals and exponent allowed). Finite values only.</summary>
    public static bool TryParse(ReadOnlySpan<char> text, out double value)
    {
        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
                                    NumberStyles.AllowExponent;
        if (double.TryParse(text, styles, CultureInfo.InvariantCulture, out value) && double.IsFinite(value))
        {
            return true;
        }

        value = 0;
        return false;
    }
}

