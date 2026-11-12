namespace PicView.Core.Svg;

/// <summary>Converts SVG text to the AXAML text of an Avalonia <c>DrawingImage</c>.</summary>
public static class SvgToXamlConverter
{
    /// <summary>
    ///     Parses with <see cref="SvgParseOptions.Default" /> and writes the drawing. Returns <c>null</c> and the reason
    ///     when the SVG is unsupported.
    /// </summary>
    public static string? Convert(string svgText, out string? unsupportedReason)
    {
        var result = SvgParser.Parse(svgText, SvgParseOptions.Default);
        if (result.Drawing is null)
        {
            unsupportedReason = result.UnsupportedReason ?? "unknown";
            return null;
        }

        unsupportedReason = null;
        return AxamlDrawingWriter.Write(result.Drawing);
    }
}
