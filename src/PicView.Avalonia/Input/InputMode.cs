namespace PicView.Avalonia.Input;

/// <summary>
/// The single mode a window is in, which decides what its Keybinds do.
/// Declared from lowest to highest rank: when several apply, the highest-ranked one wins.
/// </summary>
public enum InputMode
{
    Normal = 0,
    Slideshow,
    MotionPhoto,
    Crop,
    Renaming,
    Dialog
}

/// <summary>
/// What is active in a window and its active tab, from which its <see cref="InputMode"/> follows.
/// </summary>
/// <param name="IsDialogOpen">A pop-up, the drop-down menu or the main menu is open.</param>
public readonly record struct InputModeState(
    bool IsDialogOpen = false,
    bool IsRenaming = false,
    bool IsCropping = false,
    bool IsMotionPhotoActive = false,
    bool IsSlideshowRunning = false)
{
    /// <summary>
    /// Returns the highest-ranked Input Mode that applies.
    /// </summary>
    public InputMode Resolve()
    {
        if (IsDialogOpen)
        {
            return InputMode.Dialog;
        }

        if (IsRenaming)
        {
            return InputMode.Renaming;
        }

        if (IsCropping)
        {
            return InputMode.Crop;
        }

        if (IsMotionPhotoActive)
        {
            return InputMode.MotionPhoto;
        }

        return IsSlideshowRunning ? InputMode.Slideshow : InputMode.Normal;
    }
}

public static class InputModeExtensions
{
    /// <summary>
    /// Whether only the Actions <paramref name="mode"/> handles may run, with every other Keybind doing nothing.
    /// </summary>
    public static bool IsBlocking(this InputMode mode) =>
        mode is InputMode.Dialog or InputMode.Renaming or InputMode.Crop;
}
