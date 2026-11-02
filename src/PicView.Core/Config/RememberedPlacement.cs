namespace PicView.Core.Config;

/// <summary>
/// The size limits of a window, and whether the user can resize it.
/// </summary>
public readonly record struct WindowLimits(
    bool CanResize,
    double MinWidth,
    double MaxWidth,
    double MinHeight,
    double MaxHeight);

/// <summary>
/// Where a window is right now, as it is when it closes.
/// </summary>
public readonly record struct WindowBounds(double Width, double Height, int Left, int Top, bool Maximized);

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
    /// A maximized window applies nothing else.
    /// A size is applied only when the window can resize and both values are inside the limits.
    /// A position is applied only when both Top and Left are remembered, and never goes below zero.
    /// </summary>
    public static Placement Restore(IWindowProperties properties, WindowLimits limits)
    {
        if (properties.Maximized)
        {
            return new Placement(true, null, null);
        }

        (double Width, double Height)? size = null;
        if (limits.CanResize && properties.Width is { } width && properties.Height is { } height &&
            double.IsFinite(width) && double.IsFinite(height) &&
            width > limits.MinWidth && width < limits.MaxWidth &&
            height > limits.MinHeight && height < limits.MaxHeight)
        {
            size = (width, height);
        }

        (int Left, int Top)? position = null;
        if (properties.Top is { } top && properties.Left is { } left)
        {
            position = (Math.Max(0, left), Math.Max(0, top));
        }

        return new Placement(false, size, position);
    }

    /// <summary>
    /// Writes where a window was closed into its properties.
    /// The size is written only when the window can resize, is not maximized, and the values are finite and positive.
    /// A size in the properties that is not finite is cleared.
    /// </summary>
    public static void Remember(IWindowProperties properties, WindowBounds bounds, bool canResize)
    {
        properties.Maximized = bounds.Maximized;
        properties.Top = bounds.Top;
        properties.Left = bounds.Left;

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
