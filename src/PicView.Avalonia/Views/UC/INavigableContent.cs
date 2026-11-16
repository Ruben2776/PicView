using PicView.Avalonia.Input;

namespace PicView.Avalonia.Views.UC;

/// <summary>
/// A viewer a <see cref="NavigableView"/> shows in its slot, beneath the gallery, the click arrows and the hover bar.
/// </summary>
public interface INavigableContent : IZoomScrollTarget
{
    /// <summary>
    /// Whether an image is shown, so that turning or flipping it means something.
    /// </summary>
    bool HasImage { get; }

    /// <summary>
    /// Whether the viewer is zoomed, so a press pans instead of navigating.
    /// </summary>
    bool IsZoomed { get; }

    /// <summary>
    /// Animates the next change of the tab's flip, as the Flip Action does; resetting it on a new image does not.
    /// </summary>
    void AnimateNextFlip();

    void ResetZoom(bool enableAnimations = true);

    void ResetZoomSlim();

    void TriggerScalingModeUpdate(bool invalidate);
}
