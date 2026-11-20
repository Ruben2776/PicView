using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.UI;
using PicView.Core.DebugTools;
using PicView.Core.Svg;

namespace PicView.Avalonia.Svg;

/// <summary>
/// Loads an SVG file for showing: the parsed <see cref="SvgDrawing"/>, or, when the parser does not support the file,
/// the ImageMagick fallback bitmap. Call <see cref="LoadAsync"/> off the UI thread.
/// </summary>
internal static class SvgImageLoader
{
    private const string CurrentColorKeyword = "currentColor";
    private const string MainTextColorKey = "MainTextColor";

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
            var options = UsesCurrentColor(text)
                ? Themed(await GetMainTextColorAsync().ConfigureAwait(false))
                : SvgParseOptions.Default;

            if (TryParse(text, options, out var drawing, out reason))
            {
                return drawing;
            }
        }
        catch (Exception e)
        {
            reason = LogFailure(e);
        }

        return Fallback(fileInfo, width, height, reason);
    }

    /// <summary>
    /// The synchronous counterpart of <see cref="LoadAsync"/>. Safe to call on the UI thread: the theme color is
    /// then read inline instead of being dispatched.
    /// </summary>
    /// <inheritdoc cref="LoadAsync"/>
    public static object? Load(FileInfo fileInfo, uint width, uint height)
    {
        string reason;
        try
        {
            var text = SvgFileReader.Read(fileInfo.FullName);
            var options = UsesCurrentColor(text) ? Themed(GetMainTextColor()) : SvgParseOptions.Default;

            if (TryParse(text, options, out var drawing, out reason))
            {
                return drawing;
            }
        }
        catch (Exception e)
        {
            reason = LogFailure(e);
        }

        return Fallback(fileInfo, width, height, reason);
    }

    private static bool TryParse(string text, SvgParseOptions options, [NotNullWhen(true)] out SvgDrawing? drawing, out string reason)
    {
        var result = SvgParser.Parse(text, options);
        drawing = result.Drawing;
        reason = result.UnsupportedReason ?? "unsupported";
        return drawing is not null;
    }

    /// <summary>Logs the exception itself, for its message and stack; its type goes with the file name later.</summary>
    private static string LogFailure(Exception e)
    {
        DebugHelper.LogDebug(nameof(SvgImageLoader), nameof(LoadAsync), e);
        return e.GetType().Name;
    }

    private static object? Fallback(FileInfo fileInfo, uint width, uint height, string reason)
    {
        DebugHelper.LogDebug(nameof(SvgImageLoader), nameof(LoadAsync), $"{fileInfo.Name}: {reason}");
        return SvgFallback.Render(fileInfo, width, height);
    }

    /// <summary>
    /// Whether the file is an icon that follows the theme: it names <c>currentColor</c> somewhere. Only then is the
    /// text color read, and only then do unset fills follow it; other files keep SVG's black.
    /// </summary>
    internal static bool UsesCurrentColor(string svgText) =>
        svgText.Contains(CurrentColorKeyword, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The options for a file that uses <c>currentColor</c>: both <c>currentColor</c> and an unset fill are the text
    /// color. In a browser the two are the same color (black), so an icon mixes them freely and they must stay equal.
    /// </summary>
    internal static SvgParseOptions Themed(SvgColor textColor) =>
        new() { CurrentColor = textColor, InitialFill = textColor };

    private static async Task<SvgColor> GetMainTextColorAsync() =>
        ToSvgColor(await Dispatcher.UIThread.InvokeAsync(static () => UIHelper.GetColor(MainTextColorKey)));

    /// <summary>Runs inline on the UI thread; from any other thread it blocks until the UI thread has read it.</summary>
    private static SvgColor GetMainTextColor() =>
        ToSvgColor(Dispatcher.UIThread.Invoke(static () => UIHelper.GetColor(MainTextColorKey)));

    private static SvgColor ToSvgColor(Color color) =>
        color.A == 0 ? SvgColor.Black : new SvgColor(color.A, color.R, color.G, color.B);
}
