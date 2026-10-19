using System.Runtime.InteropServices;
using Avalonia.Controls;
using PicView.Core.Sizing;

namespace PicView.Avalonia.WindowBehavior;

/// <summary>
/// How a window takes on its size in its Window Mode.
/// </summary>
/// <param name="SizeToContent">How the window sizes itself to its content.</param>
/// <param name="MaxWidth">The window's largest width, or NaN for no limit.</param>
/// <param name="MaxHeight">The window's largest height, or NaN for no limit.</param>
/// <param name="Width">An explicit width to set now, or null to leave the window's width alone.</param>
/// <param name="Height">An explicit height to set now, or null to leave the window's height alone.</param>
/// <param name="SettleToContentAfterRender">
/// Whether, after the next render, the window goes back to sizing itself to its content with no explicit size.
/// </param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct WindowSizingPlan(
    SizeToContent SizeToContent,
    double MaxWidth,
    double MaxHeight,
    double? Width,
    double? Height,
    bool SettleToContentAfterRender)
{
    public static WindowSizingPlan For(WindowMode mode, WindowResizeReason reason, bool isShown, Fit fit,
        ScreenSize screen) => mode switch
    {
        WindowMode.Fullscreen => Limited(screen.Width, screen.Height),
        WindowMode.Maximized => Limited(screen.WorkingAreaWidth, screen.WorkingAreaHeight),
        // A user resize ends AutoFit, so the window keeps the size the user gave it
        WindowMode.AutoFit when reason is WindowResizeReason.User => Limited(double.NaN, double.NaN),
        // Before it is shown, the window gets the Fit as its size so the image never appears in a resizing window
        WindowMode.AutoFit when !isShown => new(SizeToContent.Manual, fit.WindowWidth, fit.WindowHeight,
            fit.WindowWidth, fit.WindowHeight, true),
        WindowMode.AutoFit => new(SizeToContent.WidthAndHeight, fit.WindowWidth, fit.WindowHeight, null, null, true),
        _ => Limited(double.NaN, double.NaN)
    };

    private static WindowSizingPlan Limited(double maxWidth, double maxHeight) =>
        new(SizeToContent.Manual, maxWidth, maxHeight, null, null, false);
}
