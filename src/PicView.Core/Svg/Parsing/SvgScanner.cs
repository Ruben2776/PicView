using System.Globalization;

namespace PicView.Core.Svg.Parsing;

/// <summary>Low-level scanning shared by the SVG micro-syntaxes (path data, lists of numbers, lengths).</summary>
internal static class SvgScanner
{
    public static bool IsWhitespace(char c)
    {
        return c is ' ' or '\t' or '\r' or '\n' or '\f';
    }

    public static void SkipWhitespace(ReadOnlySpan<char> text, ref int pos)
    {
        while (pos < text.Length && IsWhitespace(text[pos]))
        {
            pos++;
        }
    }

    /// <summary>Skips whitespace with at most one comma in it. Returns whether a comma was skipped.</summary>
    public static bool SkipSeparator(ReadOnlySpan<char> text, ref int pos)
    {
        SkipWhitespace(text, ref pos);
        if (pos < text.Length && text[pos] == ',')
        {
            pos++;
            SkipWhitespace(text, ref pos);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Scans one number by the SVG grammar (sign, digits, fraction, exponent) starting at <paramref name="pos" />.
    ///     An <c>e</c> not followed by digits is left unscanned, so <c>2em</c> scans as <c>2</c>.
    /// </summary>
    public static bool TryScanNumber(ReadOnlySpan<char> text, ref int pos, out ReadOnlySpan<char> number)
    {
        var start = pos;
        var i = pos;
        if (i < text.Length && text[i] is '+' or '-')
        {
            i++;
        }

        var digits = 0;
        while (i < text.Length && char.IsAsciiDigit(text[i]))
        {
            i++;
            digits++;
        }

        if (i < text.Length && text[i] == '.')
        {
            var fractionStart = i + 1;
            var j = fractionStart;
            while (j < text.Length && char.IsAsciiDigit(text[j]))
            {
                j++;
            }

            if (digits > 0 || j > fractionStart)
            {
                digits += j - fractionStart;
                i = j;
            }
        }

        if (digits == 0)
        {
            number = default;
            return false;
        }

        if (i < text.Length && text[i] is 'e' or 'E')
        {
            var j = i + 1;
            if (j < text.Length && text[j] is '+' or '-')
            {
                j++;
            }

            var exponentStart = j;
            while (j < text.Length && char.IsAsciiDigit(text[j]))
            {
                j++;
            }

            if (j > exponentStart)
            {
                i = j;
            }
        }

        number = text[start..i];
        pos = i;
        return true;
    }

    /// <summary>Converts scanned number text to a double; false when it is not finite.</summary>
    public static bool TryConvert(ReadOnlySpan<char> number, out double value)
    {
        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
                                    NumberStyles.AllowExponent;
        return double.TryParse(number, styles, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    }

    /// <summary>Scans and converts one number; false when there is none or it is not finite.</summary>
    public static bool TryReadNumber(ReadOnlySpan<char> text, ref int pos, out double value)
    {
        if (TryScanNumber(text, ref pos, out var number))
        {
            return TryConvert(number, out value);
        }

        value = 0;
        return false;
    }

    /// <summary>Reads a list of numbers separated by whitespace and/or single commas.</summary>
    public static bool TryReadNumberList(ReadOnlySpan<char> text, List<double> into)
    {
        var pos = 0;
        SkipWhitespace(text, ref pos);
        while (pos < text.Length)
        {
            if (!TryReadNumber(text, ref pos, out var value))
            {
                return false;
            }

            into.Add(value);
            var comma = SkipSeparator(text, ref pos);
            if (comma && pos >= text.Length)
            {
                return false;
            }
        }

        return true;
    }
}
