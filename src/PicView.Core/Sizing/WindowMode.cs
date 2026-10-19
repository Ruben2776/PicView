namespace PicView.Core.Sizing;

/// <summary>
/// Who owns a window's size: the screen, the work area, the shown image, or the user.
/// </summary>
public enum WindowMode
{
    Manual,
    AutoFit,
    Maximized,
    Fullscreen
}

public static class WindowModes
{
    /// <summary>
    /// Fullscreen wins over Maximized, and Maximized over AutoFit.
    /// </summary>
    public static WindowMode From(bool fullscreen, bool maximized, bool autoFit)
    {
        if (fullscreen)
        {
            return WindowMode.Fullscreen;
        }

        if (maximized)
        {
            return WindowMode.Maximized;
        }

        return autoFit ? WindowMode.AutoFit : WindowMode.Manual;
    }

    /// <summary>
    /// Where the image is fitted in this mode. A maximized window fits the image to itself, as a manual one does.
    /// </summary>
    public static FitMode ToFitMode(this WindowMode mode) => mode switch
    {
        WindowMode.Fullscreen => FitMode.Fullscreen,
        WindowMode.AutoFit => FitMode.AutoFit,
        _ => FitMode.FixedWindow
    };
}
