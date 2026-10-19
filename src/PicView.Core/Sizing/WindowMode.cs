namespace PicView.Core.Sizing;

/// <summary>
/// The state a window is in, exactly one at a time. A Normal window follows the shown image with AutoFit,
/// else keeps the size the user gave it.
/// </summary>
public enum WindowMode
{
    Normal,
    Minimized,
    Maximized,
    Fullscreen
}

public static class WindowModes
{
    /// <summary>
    /// Fullscreen wins over Maximized.
    /// </summary>
    public static WindowMode From(bool fullscreen, bool maximized)
    {
        if (fullscreen)
        {
            return WindowMode.Fullscreen;
        }

        return maximized ? WindowMode.Maximized : WindowMode.Normal;
    }

    /// <summary>
    /// Where the image is fitted in this mode. A maximized window fits the image to itself, as a Normal window
    /// without AutoFit does.
    /// </summary>
    public static FitMode ToFitMode(this WindowMode mode, bool autoFit) => mode switch
    {
        WindowMode.Fullscreen => FitMode.Fullscreen,
        WindowMode.Normal when autoFit => FitMode.AutoFit,
        _ => FitMode.FixedWindow
    };
}
