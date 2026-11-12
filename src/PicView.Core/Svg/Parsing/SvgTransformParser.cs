namespace PicView.Core.Svg.Parsing;

/// <summary>
///     The SVG <c>transform</c> attribute: a list of <c>matrix</c>, <c>translate</c>, <c>scale</c>, <c>rotate</c>,
///     <c>skewX</c> and <c>skewY</c>, separated by whitespace and/or commas.
/// </summary>
internal static class SvgTransformParser
{
    /// <summary>
    ///     Parses a transform list. <paramref name="transform" /> is null for an empty list or one that comes to the
    ///     identity. False when the list does not parse or the matrix it comes to has a member that is not finite.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> text, out SvgMatrix? transform)
    {
        transform = null;
        var total = SvgMatrix.Identity;
        Span<double> args = stackalloc double[6];
        var pos = 0;
        SvgScanner.SkipWhitespace(text, ref pos);
        while (pos < text.Length)
        {
            var nameStart = pos;
            while (pos < text.Length && char.IsAsciiLetter(text[pos]))
            {
                pos++;
            }

            var name = text[nameStart..pos];
            SvgScanner.SkipWhitespace(text, ref pos);
            if (name.IsEmpty || pos >= text.Length || text[pos] != '(')
            {
                return false;
            }

            pos++;
            if (!TryReadArguments(text, ref pos, args, out var count) ||
                !TryCreate(name, args[..count], out var matrix))
            {
                return false;
            }

            // In "A B" a point is mapped by B first, then by A.
            total = matrix.Then(total);
            var comma = SvgScanner.SkipSeparator(text, ref pos);
            if (comma && pos >= text.Length)
            {
                return false;
            }
        }

        if (!total.IsFinite)
        {
            return false;
        }

        if (!total.IsIdentity)
        {
            transform = total;
        }

        return true;
    }

    private static bool TryReadArguments(ReadOnlySpan<char> text, ref int pos, Span<double> args, out int count)
    {
        count = 0;
        SvgScanner.SkipWhitespace(text, ref pos);
        while (pos < text.Length && text[pos] != ')')
        {
            if (count == args.Length || !SvgScanner.TryReadNumber(text, ref pos, out args[count]))
            {
                return false;
            }

            count++;
            var comma = SvgScanner.SkipSeparator(text, ref pos);
            if (comma && (pos >= text.Length || text[pos] == ')'))
            {
                return false;
            }
        }

        if (pos >= text.Length)
        {
            return false;
        }

        pos++;
        return true;
    }

    private static bool TryCreate(ReadOnlySpan<char> name, ReadOnlySpan<double> a, out SvgMatrix matrix)
    {
        matrix = SvgMatrix.Identity;
        switch (name)
        {
            case "matrix" when a.Length == 6:
                matrix = new SvgMatrix(a[0], a[1], a[2], a[3], a[4], a[5]);
                return true;
            case "translate" when a.Length is 1 or 2:
                matrix = new SvgMatrix(1, 0, 0, 1, a[0], a.Length == 2 ? a[1] : 0);
                return true;
            case "scale" when a.Length is 1 or 2:
                matrix = new SvgMatrix(a[0], 0, 0, a.Length == 2 ? a[1] : a[0], 0, 0);
                return true;
            case "rotate" when a.Length is 1 or 3:
                var radians = a[0] * Math.PI / 180;
                var cos = Math.Cos(radians);
                var sin = Math.Sin(radians);
                var rotation = new SvgMatrix(cos, sin, -sin, cos, 0, 0);
                if (a.Length == 3)
                {
                    // translate(cx, cy) rotate(angle) translate(-cx, -cy)
                    rotation = new SvgMatrix(1, 0, 0, 1, -a[1], -a[2])
                        .Then(rotation)
                        .Then(new SvgMatrix(1, 0, 0, 1, a[1], a[2]));
                }

                matrix = rotation;
                return true;
            case "skewX" when a.Length == 1:
                matrix = new SvgMatrix(1, 0, Math.Tan(a[0] * Math.PI / 180), 1, 0, 0);
                return true;
            case "skewY" when a.Length == 1:
                matrix = new SvgMatrix(1, Math.Tan(a[0] * Math.PI / 180), 0, 1, 0, 0);
                return true;
            default:
                return false;
        }
    }
}
