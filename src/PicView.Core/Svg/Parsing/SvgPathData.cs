using System.Text;

namespace PicView.Core.Svg.Parsing;

/// <summary>
///     Writes path data in the one form the model uses: each command is its letter directly followed by its numbers
///     joined by commas, commands are separated by one space, and the whole is prefixed <c>F1 </c> for nonzero.
/// </summary>
internal static class SvgPathData
{
    private const string NonZeroPrefix = "F1 ";

    /// <summary>
    ///     Tokenizes <paramref name="d" /> by the SVG path grammar and writes it back in the model's form, keeping every
    ///     number as written. Each implicit repeat gets its own letter. False when <paramref name="d" /> does not
    ///     tokenize or does not start with a move.
    /// </summary>
    public static bool TryNormalize(string d, bool nonZero, out string pathData)
    {
        pathData = string.Empty;
        var text = d.AsSpan();
        var builder = new StringBuilder(d.Length + 16);
        if (nonZero)
        {
            builder.Append(NonZeroPrefix);
        }

        var pos = 0;
        SvgScanner.SkipWhitespace(text, ref pos);
        if (pos >= text.Length || text[pos] is not ('M' or 'm'))
        {
            return false;
        }

        var first = true;
        while (pos < text.Length)
        {
            var command = text[pos];
            var argumentCount = ArgumentCount(command);
            if (argumentCount < 0)
            {
                return false;
            }

            pos++;
            SvgScanner.SkipWhitespace(text, ref pos);

            if (argumentCount == 0)
            {
                AppendCommand(builder, command, ref first);
                continue;
            }

            // The command's first set of arguments, then any implicit repeats.
            var repeatCommand = command switch
            {
                'M' => 'L',
                'm' => 'l',
                _ => command
            };
            var letter = command;
            do
            {
                AppendCommand(builder, letter, ref first);
                if (!TryAppendArguments(text, ref pos, builder, command, argumentCount))
                {
                    return false;
                }

                letter = repeatCommand;
                var comma = SvgScanner.SkipSeparator(text, ref pos);
                if (comma && (pos >= text.Length || !StartsNumber(text[pos])))
                {
                    return false;
                }
            } while (pos < text.Length && StartsNumber(text[pos]));
        }

        pathData = builder.ToString();
        return true;
    }

    public static string Rect(double x, double y, double width, double height, double rx, double ry, bool nonZero)
    {
        var builder = Start(nonZero);
        if (rx <= 0 || ry <= 0)
        {
            builder.Append('M').Append(F(x)).Append(',').Append(F(y))
                .Append(" H").Append(F(x + width))
                .Append(" V").Append(F(y + height))
                .Append(" H").Append(F(x))
                .Append(" Z");
            return builder.ToString();
        }

        rx = Math.Min(rx, width / 2);
        ry = Math.Min(ry, height / 2);
        builder.Append('M').Append(F(x + rx)).Append(',').Append(F(y))
            .Append(" H").Append(F(x + width - rx));
        AppendCorner(builder, rx, ry, x + width, y + ry);
        builder.Append(" V").Append(F(y + height - ry));
        AppendCorner(builder, rx, ry, x + width - rx, y + height);
        builder.Append(" H").Append(F(x + rx));
        AppendCorner(builder, rx, ry, x, y + height - ry);
        builder.Append(" V").Append(F(y + ry));
        AppendCorner(builder, rx, ry, x + rx, y);
        builder.Append(" Z");
        return builder.ToString();
    }

    public static string Circle(double cx, double cy, double r, bool nonZero)
    {
        return Ellipse(cx, cy, r, r, nonZero);
    }

    public static string Ellipse(double cx, double cy, double rx, double ry, bool nonZero)
    {
        var builder = Start(nonZero);
        var radii = F(rx) + "," + F(ry) + ",0,1,0,";
        builder.Append('M').Append(F(cx - rx)).Append(',').Append(F(cy))
            .Append(" A").Append(radii).Append(F(cx + rx)).Append(',').Append(F(cy))
            .Append(" A").Append(radii).Append(F(cx - rx)).Append(',').Append(F(cy))
            .Append(" Z");
        return builder.ToString();
    }

    public static string Line(double x1, double y1, double x2, double y2, bool nonZero)
    {
        var builder = Start(nonZero);
        builder.Append('M').Append(F(x1)).Append(',').Append(F(y1))
            .Append(" L").Append(F(x2)).Append(',').Append(F(y2));
        return builder.ToString();
    }

    /// <summary>Points as x,y pairs; needs at least two points.</summary>
    public static string Polyline(IReadOnlyList<double> points, bool nonZero)
    {
        return Poly(points, false, nonZero);
    }

    /// <summary>Points as x,y pairs; needs at least two points.</summary>
    public static string Polygon(IReadOnlyList<double> points, bool nonZero)
    {
        return Poly(points, true, nonZero);
    }

    private static string Poly(IReadOnlyList<double> points, bool closed, bool nonZero)
    {
        var builder = Start(nonZero);
        for (var i = 0; i + 1 < points.Count; i += 2)
        {
            if (i > 0)
            {
                builder.Append(" L");
            }
            else
            {
                builder.Append('M');
            }

            builder.Append(F(points[i])).Append(',').Append(F(points[i + 1]));
        }

        if (closed)
        {
            builder.Append(" Z");
        }

        return builder.ToString();
    }

    private static StringBuilder Start(bool nonZero)
    {
        var builder = new StringBuilder(64);
        if (nonZero)
        {
            builder.Append(NonZeroPrefix);
        }

        return builder;
    }

    private static void AppendCorner(StringBuilder builder, double rx, double ry, double x, double y)
    {
        builder.Append(" A").Append(F(rx)).Append(',').Append(F(ry)).Append(",0,0,1,")
            .Append(F(x)).Append(',').Append(F(y));
    }

    private static string F(double value)
    {
        return SvgNumber.Format(value);
    }

    private static int ArgumentCount(char command)
    {
        return command switch
        {
            'M' or 'm' or 'L' or 'l' or 'T' or 't' => 2,
            'H' or 'h' or 'V' or 'v' => 1,
            'C' or 'c' => 6,
            'S' or 's' or 'Q' or 'q' => 4,
            'A' or 'a' => 7,
            'Z' or 'z' => 0,
            _ => -1
        };
    }

    private static bool StartsNumber(char c)
    {
        return char.IsAsciiDigit(c) || c is '+' or '-' or '.';
    }

    private static void AppendCommand(StringBuilder builder, char letter, ref bool first)
    {
        if (!first)
        {
            builder.Append(' ');
        }

        builder.Append(letter);
        first = false;
    }

    private static bool TryAppendArguments(ReadOnlySpan<char> text, ref int pos, StringBuilder builder, char command,
        int argumentCount)
    {
        var isArc = command is 'A' or 'a';
        for (var i = 0; i < argumentCount; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
                var comma = SvgScanner.SkipSeparator(text, ref pos);
                if (comma && pos >= text.Length)
                {
                    return false;
                }
            }

            if (isArc && i is 3 or 4)
            {
                // Arc flags are a single 0 or 1 and need no separator after them.
                if (pos >= text.Length || text[pos] is not ('0' or '1'))
                {
                    return false;
                }

                builder.Append(text[pos]);
                pos++;
                continue;
            }

            if (!SvgScanner.TryScanNumber(text, ref pos, out var number) || !SvgScanner.TryConvert(number, out _))
            {
                return false;
            }

            builder.Append(number);
        }

        return true;
    }
}
