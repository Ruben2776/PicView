using PicView.Core.Keybindings;

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

    // The expanded gallery covers the image, so it ranks above what the image or the slideshow is doing
    Gallery,
    Crop,
    Renaming,
    Dialog
}

/// <summary>
/// What is active in a window and its active tab, from which its <see cref="InputMode"/> follows.
/// </summary>
/// <param name="IsDialogOpen">A pop-up, the drop-down menu or the main menu is open.</param>
/// <param name="IsAnimatedImageActive">The active tab shows an animated image.</param>
/// <param name="IsGalleryExpanded">The active tab's gallery is shown expanded.</param>
public readonly record struct InputModeState(
    bool IsDialogOpen = false,
    bool IsRenaming = false,
    bool IsCropping = false,
    bool IsMotionPhotoActive = false,
    bool IsSlideshowRunning = false,
    bool IsAnimatedImageActive = false,
    bool IsGalleryExpanded = false)
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

        if (IsGalleryExpanded)
        {
            return InputMode.Gallery;
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
}

public static class InputModeExtensions
{
    extension(InputMode mode)
    {
        /// <summary>
        /// Whether only the Actions <paramref name="mode"/> handles or allows may run, with every other Keybind
        /// doing nothing.
        /// </summary>
        public bool IsBlocking() =>
            mode is InputMode.Dialog or InputMode.Renaming or InputMode.Crop;

        /// <summary>
        /// Whether the blocking <paramref name="mode"/> lets the ordinary <paramref name="action"/> run there.
        /// The crop view zooms (ADR-0027) and turns with its tab, so Crop handles the zoom Actions and the turn and
        /// flip Actions. A window works outside the main view, so Crop and Dialog let Window Actions run (ADR-0029).
        /// Renaming allows none.
        /// </summary>
        public bool Allows(string action) =>
            mode switch
            {
                InputMode.Crop => action is Actions.ZoomIn or Actions.ZoomOut or Actions.ResetZoom
                                      or Actions.RotateLeft or Actions.RotateRight or Actions.Rotate0
                                      or Actions.Rotate90 or Actions.Rotate180 or Actions.Rotate270 or Actions.Flip ||
                                  KeybindCategoryDefinition.IsWindowAction(action),
                InputMode.Dialog => KeybindCategoryDefinition.IsWindowAction(action),
                _ => false
            };

        /// <summary>
        /// Whether <paramref name="mode"/> is a non-blocking mode other than Normal, which disallows Actions
        /// that need a still image.
        /// </summary>
        public bool DisallowsStillImageActions() =>
            mode is InputMode.Gallery or InputMode.MotionPhoto or InputMode.AnimatedImage or InputMode.Slideshow;
    }
}
