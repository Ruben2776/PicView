namespace PicView.Avalonia.Input;

/// <summary>
/// The single mode a window is in, which decides what its Keybinds do.
/// Declared from lowest to highest rank: when several apply, the highest-ranked one wins.
/// </summary>
public enum InputMode
{
    Normal = 0,
    Slideshow,

    // An image is a motion photo or an animated image, never both
    AnimatedImage,
    MotionPhoto,
    Crop,
    Renaming,
    Dialog
}

/// <summary>
/// What is active in a window and its active tab, from which its <see cref="InputMode"/> follows.
/// </summary>
/// <param name="IsDialogOpen">A pop-up, the drop-down menu or the main menu is open.</param>
/// <param name="IsAnimatedImageActive">The active tab shows an animated image.</param>
public readonly record struct InputModeState(
    bool IsDialogOpen = false,
    bool IsRenaming = false,
    bool IsCropping = false,
    bool IsMotionPhotoActive = false,
    bool IsSlideshowRunning = false,
    bool IsAnimatedImageActive = false)
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

        if (IsAnimatedImageActive)
        {
            return InputMode.AnimatedImage;
        }

        return IsSlideshowRunning ? InputMode.Slideshow : InputMode.Normal;
    }

    /// <summary>
    /// The modes asked in turn to give a Contextual Action a meaning: a blocking mode alone, or every active
    /// non-blocking mode from highest rank down, followed by <see cref="InputMode.Normal"/>.
    /// </summary>
    public IReadOnlyList<InputMode> ContextualChain()
    {
        var mode = Resolve();
        if (mode.IsBlocking())
        {
            return [mode];
        }

        var chain = new List<InputMode>(3);
        if (IsMotionPhotoActive)
        {
            chain.Add(InputMode.MotionPhoto);
        }

        if (IsAnimatedImageActive)
        {
            chain.Add(InputMode.AnimatedImage);
        }

        if (IsSlideshowRunning)
        {
            chain.Add(InputMode.Slideshow);
        }

        chain.Add(InputMode.Normal);
        return chain;
    }
}

public static class InputModeExtensions
{
    extension(InputMode mode)
    {
        /// <summary>
        /// Whether only the Actions <paramref name="mode"/> handles may run, with every other Keybind doing nothing.
        /// </summary>
        public bool IsBlocking() =>
            mode is InputMode.Dialog or InputMode.Renaming or InputMode.Crop;

        /// <summary>
        /// Whether <paramref name="mode"/> is a non-blocking mode other than Normal, which disallows Actions
        /// that need a still image.
        /// </summary>
        public bool DisallowsStillImageActions() =>
            mode is InputMode.MotionPhoto or InputMode.AnimatedImage or InputMode.Slideshow;
    }
}
