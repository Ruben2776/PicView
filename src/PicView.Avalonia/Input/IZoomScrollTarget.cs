using Avalonia;

namespace PicView.Avalonia.Input;

/// <summary>
/// What the mouse wheel scrolls and zooms in a window: its image viewer.
/// </summary>
public interface IZoomScrollTarget
{
    /// <summary>
    /// Whether the shown image is taller than the viewer, so the wheel scrolls it instead of running an Action.
    /// </summary>
    bool CanScrollVertically { get; }

    void ScrollLine(bool down);

    /// <summary>
    /// Zooms in or out towards <paramref name="pointer"/>, in the viewer's zoom control's coordinates.
    /// </summary>
    void ZoomAt(bool zoomIn, Point pointer);
}
