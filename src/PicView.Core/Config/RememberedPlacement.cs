namespace PicView.Core.Config;

/// <summary>
/// The size limits of a window, whether the user can resize it, and the size it has before it is placed.
/// </summary>
/// <param name="Width">The window's own width, NaN when it is not set.</param>
/// <param name="Height">The window's own height, NaN when it is not set.</param>
public readonly record struct WindowLimits(
    bool CanResize,
    double MinWidth,
    double MaxWidth,
    double MinHeight,
    double MaxHeight,
    double Width,
    double Height);

/// <summary>
/// Where a window is right now, as it is when it closes.
/// </summary>
public readonly record struct WindowBounds(
    double Width,
    double Height,
    int Left,
    int Top,
    bool Maximized,
    bool Minimized);

/// <summary>
/// A screen's area, in the same pixels as a window's position.
/// </summary>
public readonly record struct ScreenArea(int X, int Y, int Width, int Height);

/// <summary>
/// What to apply to a window that is opening. A null part is left to the window's own default.
/// </summary>
public readonly record struct Placement(
    bool Maximize,
    (double Width, double Height)? Size,
    (int Left, int Top)? Position);

/// <summary>
/// The rule a Tool Window or App Window is placed and remembered by.
/// Both directions live here so they stay mirror images of each other.
/// </summary>
public static class RememberedPlacement
{
    /// <summary>
    /// Works out what to apply to a window that is opening from what it remembered.
    /// A maximized window maximizes, and keeps the size and position that pass their checks for when it is restored.
    /// A size is applied only when the window can resize and both values are finite and inside the limits, inclusive.
    /// A position is applied only when both Top and Left are remembered and the window would overlap one of the
    /// screens. With no screens known, a remembered position is applied as it is.
    /// </summary>
    public static Placement Restore(IWindowProperties properties, WindowLimits limits,
        ReadOnlySpan<ScreenArea> screens)
    {
        (double Width, double Height)? size = null;
        if (limits.CanResize && properties.Width is { } width && properties.Height is { } height &&
            double.IsFinite(width) && double.IsFinite(height) &&
            width >= limits.MinWidth && width <= limits.MaxWidth &&
            height >= limits.MinHeight && height <= limits.MaxHeight)
        {
            size = (width, height);
        }

        (int Left, int Top)? position = null;
        if (properties.Top is { } top && properties.Left is { } left)
        {
            var (testWidth, testHeight) = size ?? (limits.Width, limits.Height);
            if (IsOnAScreen(left, top, testWidth, testHeight, screens))
            {
                position = (left, top);
            }
        }

        return new Placement(properties.Maximized, size, position);
    }

    /// <summary>
    /// Whether a window at this position and size overlaps any of the screens. A size that is not a number counts
    /// as one pixel. With no screens known, every position counts as on a screen.
    /// </summary>
    private static bool IsOnAScreen(int left, int top, double width, double height, ReadOnlySpan<ScreenArea> screens)
    {
        if (screens.IsEmpty)
        {
            return true;
        }

        var right = left + (double.IsFinite(width) && width > 0 ? width : 1);
        var bottom = top + (double.IsFinite(height) && height > 0 ? height : 1);
        foreach (var screen in screens)
        {
            if (left < screen.X + screen.Width && right > screen.X &&
                top < screen.Y + screen.Height && bottom > screen.Y)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Writes where a window was closed into its properties.
    /// The position is written only when the window is not minimized, since a minimized window has no real position.
    /// The size is written only when the window can resize, is not maximized, and the values are finite and positive.
    /// A size in the properties that is not finite is cleared.
    /// </summary>
    public static void Remember(IWindowProperties properties, WindowBounds bounds, bool canResize)
    {
        properties.Maximized = bounds.Maximized;
        if (!bounds.Minimized)
        {
            properties.Top = bounds.Top;
            properties.Left = bounds.Left;
        }

        if (canResize && !bounds.Maximized)
        {
            if (double.IsFinite(bounds.Width) && bounds.Width > 0)
            {
                properties.Width = bounds.Width;
            }

            if (double.IsFinite(bounds.Height) && bounds.Height > 0)
            {
                properties.Height = bounds.Height;
            }
        }

        if (properties.Width is { } oldWidth && !double.IsFinite(oldWidth))
        {
            properties.Width = null;
        }

        if (properties.Height is { } oldHeight && !double.IsFinite(oldHeight))
        {
            properties.Height = null;
        }
    }
}
