namespace PicView.Core.IPlatform;

/// <summary>
/// Changes one window's Window Mode, and saves the mode the settings remember when the user changed it.
/// </summary>
public interface IWindowModeSwitcher
{
    ValueTask ToggleFullscreen();

    ValueTask ToggleMaximized();

    /// <summary>
    /// Leaves the mode on top, as the Restore button does.
    /// </summary>
    ValueTask LeaveTopMode();

    /// <summary>
    /// Turns AutoFit on or off for this window. Turning it on or off from the menus applies to every window.
    /// </summary>
    ValueTask SetAutoFit(bool autoFit);

    /// <summary>
    /// The user resized the window by hand, which turns AutoFit off for only this window.
    /// </summary>
    ValueTask ResizedByHand();

    /// <summary>
    /// Shows the window fullscreen for a slideshow, without changing the remembered mode.
    /// </summary>
    ValueTask EnterSlideshow();

    /// <summary>
    /// Gives back the mode the window had before the slideshow.
    /// </summary>
    ValueTask LeaveSlideshow();
}
