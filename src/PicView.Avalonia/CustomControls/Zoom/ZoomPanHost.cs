namespace PicView.Avalonia.CustomControls.Zoom;

/// <summary>
/// Which view a <see cref="ZoomPanControl"/> zooms, which decides when a press pans (ADR-0027).
/// </summary>
public enum ZoomPanHost
{
    /// <summary>A left press pans a zoomed image; a press with Shift alone is left for moving the window.</summary>
    ImageViewer,

    /// <summary>
    /// A left press pans a zoomed image only while Space is held, since a plain press moves or resizes the selection.
    /// </summary>
    Crop,

    /// <summary>
    /// A left press pans a zoomed image as in the viewer, except while alignment points are being picked, when the
    /// press is the point.
    /// </summary>
    Comparison
}
