using System.Globalization;
using System.Text;

namespace PicView.Core.Svg;

public static class SvgNumber
{
    /// <summary>Formats with the invariant culture and up to three decimals; negative zero prints as "0".</summary>
    public static string Format(double value)
    {
        var text = value.ToString("0.###", CultureInfo.InvariantCulture);
        return string.Equals(text, "-0", StringComparison.Ordinal) ? "0" : text;
    }

    /// <summary>
    ///     The shortest invariant text that parses back to the same double, as a plain decimal without an exponent;
    ///     negative zero prints as "0".
    /// </summary>
    internal static string FormatRoundTrip(double value)
    {
        if (value == 0)
        {
            return "0";
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        if (text.IndexOf('E', StringComparison.Ordinal) < 0)
        {
            return text;
        }

        var builder = new StringBuilder(32);
        AppendPlain(builder, text);
        return builder.ToString();
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

    /// <summary>
    ///     Appends number text in the SVG grammar (sign, digits, fraction, exponent) as written, unless it has a
    ///     leading <c>+</c>, a trailing <c>.</c> or an exponent. Those are rewritten as plain decimals with the same
    ///     digits, so the value is unchanged.
    /// </summary>
    internal static void AppendPlain(StringBuilder builder, ReadOnlySpan<char> number)
    {
        if (number.IndexOfAny("+eE") < 0 && number[^1] != '.')
        {
            builder.Append(number);
            return;
        }

        var negative = number[0] == '-';
        if (number[0] is '+' or '-')
        {
            number = number[1..];
        }

        var exponent = 0;
        var e = number.IndexOfAny('e', 'E');
        var exponentText = e >= 0 ? number[(e + 1)..] : ReadOnlySpan<char>.Empty;
        if (e >= 0)
        {
            number = number[..e];
        }

        var point = number.IndexOf('.');
        var integerDigits = point < 0 ? number : number[..point];
        var fractionDigits = point < 0 ? ReadOnlySpan<char>.Empty : number[(point + 1)..];
        var digits = string.Concat(integerDigits, fractionDigits).TrimStart('0');
        if (digits.Length == 0)
        {
            builder.Append('0');
            return;
        }

        // The scanner only leaves an e in the number when digits follow it. An exponent past the int range, with
        // nonzero digits, can only be finite if it is negative (no string holds 2^31 leading zeros), and then the
        // number is below the smallest double, so it parses as 0.
        if (e >= 0 && !int.TryParse(exponentText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
                out exponent))
        {
            builder.Append('0');
            return;
        }

        // The decimal point's place relative to the first significant digit: the number is 0.digits × 10^this.
        var leadingZeros = integerDigits.Length + fractionDigits.Length - digits.Length;
        var pointPosition = (long)integerDigits.Length - leadingZeros + exponent;

        // Below 1e-400 a number parses as 0, the same value as "0". A finite number never reaches 1e309, so
        // pointPosition never exceeds 309 here and the expansion below stays short.
        if (pointPosition < -400)
        {
            builder.Append('0');
            return;
        }

        if (negative)
        {
            builder.Append('-');
        }

        if (pointPosition <= 0)
        {
            builder.Append("0.").Append('0', (int)-pointPosition).Append(digits);
        }
        else if (pointPosition >= digits.Length)
        {
            builder.Append(digits).Append('0', (int)(pointPosition - digits.Length));
        }
        else
        {
            builder.Append(digits.AsSpan(0, (int)pointPosition)).Append('.').Append(digits.AsSpan((int)pointPosition));
        }
    }
}
