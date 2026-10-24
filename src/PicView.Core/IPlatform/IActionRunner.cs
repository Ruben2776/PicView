namespace PicView.Core.IPlatform;

/// <summary>
/// The window's one seam for running Actions, plus the few operations that are not Actions.
/// </summary>
public interface IActionRunner
{
    /// <summary>
    /// Runs the Action named <paramref name="action"/>, one of <see cref="Keybindings.Actions"/>. An unknown name is
    /// logged and does nothing.
    /// </summary>
    ValueTask Run(string action);

    /// <summary>
    /// Opens <paramref name="path"/>, such as a favorite or a file history entry.
    /// </summary>
    ValueTask OpenPathAsync(string path);

    /// <summary>
    /// Stops the navigation a held Keybind repeats, when it is released.
    /// </summary>
    ValueTask StopRepeatedNavigation();

    /// <summary>
    /// Shows the start-up menu, such as when the shown folder is emptied.
    /// </summary>
    ValueTask ShowStartUpMenu();
}
