using Avalonia;
using Avalonia.Input;
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

    /// <summary>
    /// Where the pointer of <paramref name="e"/> is, in the coordinates <see cref="IZoomScrollTarget.ZoomAt"/> takes.
    /// The navigable view routes the wheel, so a wheel over its chrome zooms the viewer too.
    /// </summary>
    Point ZoomPointer(PointerEventArgs e);

    /// <summary>
    /// Zooms for a touchpad magnify or a pinch, which the navigable view routes here from anywhere over its content.
    /// </summary>
    void Magnify(PointerDeltaEventArgs e);

    /// <summary>
    /// The viewer's zoom preview, which the navigable view shows above its chrome while the viewer is shown, or
    /// nothing when it has none.
    /// </summary>
    ZoomPreviewer? ZoomPreview { get; }
}
