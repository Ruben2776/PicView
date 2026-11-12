using Avalonia.Threading;
using PicView.Avalonia.UI;
using PicView.Core.DebugTools;
using PicView.Core.Svg;

namespace PicView.Avalonia.Svg;

/// <summary>
/// Loads an SVG file for showing: the parsed <see cref="SvgDrawing"/>, or, when the parser does not support the file,
/// the ImageMagick fallback bitmap. Call it off the UI thread.
/// </summary>
internal static class SvgImageLoader
{
    private const string CurrentColorKeyword = "currentColor";

    /// <summary>
    /// Returns an <see cref="SvgDrawing"/>, a fallback bitmap, or null when ImageMagick cannot render the file either.
    /// </summary>
    /// <param name="fileInfo">The SVG or SVGZ file.</param>
    /// <param name="width">The SVG's own width, from the ping, used for the fallback.</param>
    /// <param name="height">The SVG's own height, from the ping, used for the fallback.</param>
    public static async ValueTask<object?> LoadAsync(FileInfo fileInfo, uint width, uint height)
    {
        string reason;
        try
        {
            var text = await SvgFileReader.ReadAsync(fileInfo.FullName).ConfigureAwait(false);
            var options = SvgParseOptions.Default;
            if (text.Contains(CurrentColorKeyword, StringComparison.OrdinalIgnoreCase))
            {
                options = new SvgParseOptions { CurrentColor = await GetMainTextColorAsync().ConfigureAwait(false) };
            }

            var result = SvgParser.Parse(text, options);
            if (result.Drawing is not null)
            {
                return result.Drawing;
            }

            reason = result.UnsupportedReason ?? "unsupported";
        }
        catch (Exception e)
        {
            reason = e.Message;
        }

        DebugHelper.LogDebug(nameof(SvgImageLoader), nameof(LoadAsync), $"{fileInfo.Name}: {reason}");
        return SvgFallback.Render(fileInfo, width, height);
    }

    private static async Task<SvgColor> GetMainTextColorAsync()
    {
        var color = await Dispatcher.UIThread.InvokeAsync(() => UIHelper.GetColor("MainTextColor"));
        return color.A == 0 ? SvgColor.Black : new SvgColor(color.A, color.R, color.G, color.B);
    }
}
