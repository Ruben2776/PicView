using System.Runtime.InteropServices;

namespace PicView.Core.Sizing;

/// <summary>
/// A window's position in physical pixels, as the operating system places windows.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PixelPosition(int X, int Y);

/// <summary>
/// Where a window goes on its screen. Sizes are in device-independent pixels; positions come out in physical pixels,
/// converted once with the screen's scaling.
/// </summary>
public static class WindowPlacement
{
    /// <summary>
    /// The position that centers a window of this size in the screen's work area.
    /// </summary>
    /// <param name="top">Keeps the window at the top of the work area instead of centering it vertically.</param>
    public static PixelPosition Centered(ScreenSize screen, double width, double height, bool top = false)
    {
        var x = screen.WorkingAreaX + (screen.WorkingAreaWidth - width) / 2;
        var y = screen.WorkingAreaY + (top ? 0 : (screen.WorkingAreaHeight - height) / 2);
        return ToPixels(screen, x, y);
    }

    /// <summary>
    /// Where an AutoFit window goes when its size changes: centered in the work area with Stay centered, else where
    /// its center stays put.
    /// </summary>
    /// <param name="position">Where the window was before it changed size.</param>
    public static PixelPosition AfterResize(ScreenSize screen, PixelPosition position, double oldWidth,
        double oldHeight, double newWidth, double newHeight, bool keepCentered)
    {
        if (keepCentered)
        {
            return Centered(screen, newWidth, newHeight);
        }

        var scaling = ScalingOf(screen);
        return new PixelPosition(
            (int)(position.X + (int)(oldWidth - newWidth) / 2 * scaling),
            (int)(position.Y + (int)(oldHeight - newHeight) / 2 * scaling));
    }

    private static PixelPosition ToPixels(ScreenSize screen, double x, double y)
    {
        var scaling = ScalingOf(screen);
        return new PixelPosition((int)(x * scaling), (int)(y * scaling));
    }

    private static double ScalingOf(ScreenSize screen) => screen.Scaling > 0 ? screen.Scaling : 1;
}
