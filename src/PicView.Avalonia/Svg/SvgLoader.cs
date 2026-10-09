using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Svg;

public static partial class SvgLoader
{
    private const double PixelsPerInch = 96.0;
    private const double PixelsPerPoint = PixelsPerInch / 72.0; // 1 point = 1/72 inch
    private const double PixelsPerPica = PixelsPerPoint * 12.0; // 1 pica = 12 points
    private const double PixelsPerCentimeter = PixelsPerInch / 2.54;
    private const double PixelsPerMillimeter = PixelsPerCentimeter / 10.0;
    private const double PixelsPerEm = 16.0; // Common default for 1em/1rem
    private const string CurrentColorKeyword = "currentColor";

    /// <summary>
    /// Preprocesses SVG string data from a file.
    /// It converts non-pixel units to pixels and replaces the "currentColor" keyword with the application's main text color.
    /// </summary>
    /// <param name="filePath">The path to the SVG file.</param>
    /// <returns>A preprocessed SVG string.</returns>
    public static async Task<string> GetContentFromSvgFileAsync(string filePath)
    {
        var svgMarkup = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

        svgMarkup = ConvertUnitsToPixels(svgMarkup);

        if (!svgMarkup.Contains(CurrentColorKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return svgMarkup;
        }

        svgMarkup = await ReplaceCurrentColorAsync(svgMarkup).ConfigureAwait(false);
        return svgMarkup;
    }

    private static async Task<string> ReplaceCurrentColorAsync(string svgMarkup)
    {
        // If the SVG's "currentColor" exists, replace it with the main text color
        var color = await Dispatcher.UIThread.InvokeAsync(() => UIHelper.GetColor("MainTextColor"));
        var hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        return CurrentColorRegex().Replace(svgMarkup, hexColor);
    }

    /// <summary>
    /// Finds numeric values with common non-pixel units (em, rem, pt, in, etc.)
    /// and converts them to their pixel equivalent.
    /// </summary>
    private static string ConvertUnitsToPixels(string svgContent)
    {
        // This regex captures a number (integer or decimal) followed by a unit
        var unitRegex = UnitWithNumberRegex();
        return unitRegex.Replace(svgContent, match =>
        {
            // Parse the numeric part of the match
            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return match.Value; // Return original if parsing fails
            }

            var unit = match.Groups[2].Value.ToLowerInvariant();
            var pixelValue = unit switch
            {
                "em" or "rem" => value * PixelsPerEm,
                "pt" => value * PixelsPerPoint,
                "pc" => value * PixelsPerPica,
                "in" => value * PixelsPerInch,
                "cm" => value * PixelsPerCentimeter,
                "mm" => value * PixelsPerMillimeter,
                _ => value // Should not happen with this regex
            };

            // Return the value as a string, rounded to 2 decimal places.
            // SVG treats unitless numbers as pixels.
            return pixelValue.ToString("F2", CultureInfo.InvariantCulture);
        });
    }

    [GeneratedRegex(@"(-?\d*\.?\d+)(em|rem|pt|pc|in|cm|mm)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "en-US")]
    private static partial Regex UnitWithNumberRegex();

    [GeneratedRegex(CurrentColorKeyword, RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex CurrentColorRegex();
}