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
    /// Shows the window fullscreen for a slideshow, without changing the remembered mode.
    /// </summary>
    ValueTask EnterSlideshow();

    /// <summary>
    /// Gives back the mode the window had before the slideshow.
    /// </summary>
    ValueTask LeaveSlideshow();
}
