using System.Globalization;
using System.Text;

namespace PicView.Benchmarks.Svg;

/// <summary>
/// Three generated SVG files that use only features the native SVG parser converts: a small icon, a CSS-styled export
/// and a large map. They avoid <c>currentColor</c> so loading never hops to the UI thread.
/// </summary>
public static class SvgBenchmarkFiles
{
    private const string Namespace = "http://www.w3.org/2000/svg";

    public static string Icon { get; } =
        $"""
         <svg xmlns="{Namespace}" width="24" height="24" viewBox="0 0 24 24">
           <path d="M12 2 L22 12 L12 22 L2 12 Z M12 7 L17 12 L12 17 L7 12 Z" fill="#336699" fill-rule="evenodd"/>
         </svg>
         """;

    public static string CssExport { get; } = BuildCssExport();

    public static string LargeMap { get; } = BuildLargeMap();

    /// <summary>
    /// Writes the three files into <paramref name="directory"/> and returns their paths in the order icon, CSS
    /// export, large map.
    /// </summary>
    public static (string Icon, string CssExport, string LargeMap) WriteAll(string directory)
    {
        Directory.CreateDirectory(directory);
        var icon = Path.Combine(directory, "icon.svg");
        var cssExport = Path.Combine(directory, "css-export.svg");
        var largeMap = Path.Combine(directory, "large-map.svg");
        File.WriteAllText(icon, Icon);
        File.WriteAllText(cssExport, CssExport);
        File.WriteAllText(largeMap, LargeMap);
        return (icon, cssExport, largeMap);
    }

    private static string BuildCssExport()
    {
        var random = new Random(42);
        var builder = new StringBuilder();
        builder.AppendLine($"<svg xmlns=\"{Namespace}\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"800\" height=\"600\" viewBox=\"0 0 800 600\">");
        builder.AppendLine("  <defs>");
        builder.AppendLine("    <style>");
        builder.AppendLine("      .a { fill: #e74c3c; stroke: #2c3e50; stroke-width: 1; }");
        builder.AppendLine("      .b { fill: #3498db; opacity: 0.8; }");
        builder.AppendLine("      .c { fill: url(#grad); stroke: #1b2631; stroke-width: 0.5; }");
        builder.AppendLine("      .d { fill: #2ecc71; stroke: none; }");
        builder.AppendLine("      circle.e { fill: #f1c40f; stroke: #7d6608; stroke-width: 2; }");
        builder.AppendLine("      rect, ellipse { fill-opacity: 0.9; }");
        builder.AppendLine("    </style>");
        builder.AppendLine("    <linearGradient id=\"grad\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"1\">");
        builder.AppendLine("      <stop offset=\"0\" stop-color=\"#8e44ad\"/>");
        builder.AppendLine("      <stop offset=\"1\" stop-color=\"#f39c12\"/>");
        builder.AppendLine("    </linearGradient>");
        builder.AppendLine("    <clipPath id=\"clip\">");
        builder.AppendLine("      <rect x=\"20\" y=\"20\" width=\"760\" height=\"560\" rx=\"20\" ry=\"20\"/>");
        builder.AppendLine("    </clipPath>");
        builder.AppendLine("  </defs>");
        builder.AppendLine("  <g clip-path=\"url(#clip)\">");
        for (var i = 0; i < 200; i++)
        {
            var x = random.Next(0, 760);
            var y = random.Next(0, 560);
            var size = random.Next(8, 40);
            switch (i % 4)
            {
                case 0:
                    Append(builder, $"    <rect class=\"a\" x=\"{x}\" y=\"{y}\" width=\"{size}\" height=\"{size}\" rx=\"3\"/>");
                    break;
                case 1:
                    Append(builder, $"    <circle class=\"e b\" cx=\"{x}\" cy=\"{y}\" r=\"{size / 2}\"/>");
                    break;
                case 2:
                    Append(builder, $"    <ellipse class=\"c\" cx=\"{x}\" cy=\"{y}\" rx=\"{size}\" ry=\"{size / 2}\"/>");
                    break;
                default:
                    Append(builder, $"    <path class=\"d\" d=\"M{x} {y} L{x + size} {y} L{x + size / 2} {y + size} Z\"/>");
                    break;
            }
        }

        builder.AppendLine("  </g>");
        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static string BuildLargeMap()
    {
        var random = new Random(1234);
        var builder = new StringBuilder(2_500_000);
        builder.AppendLine($"<svg xmlns=\"{Namespace}\" width=\"4000\" height=\"4000\" viewBox=\"0 0 4000 4000\">");
        for (var i = 0; i < 20_000; i++)
        {
            var x = random.Next(0, 3950);
            var y = random.Next(0, 3950);
            var dx1 = random.Next(5, 50);
            var dy1 = random.Next(-25, 25);
            var dx2 = random.Next(-25, 25);
            var dy2 = random.Next(5, 50);
            var color = random.Next(0, 0x1000000);
            Append(builder,
                $"<path d=\"M{x} {y} l{dx1} {dy1} l{dx2} {dy2} l{-dx1} {-dy2} z\" fill=\"#{color:x6}\" stroke=\"#202020\" stroke-width=\"0.5\"/>");
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, FormattableString line) =>
        builder.AppendLine(line.ToString(CultureInfo.InvariantCulture));
}
