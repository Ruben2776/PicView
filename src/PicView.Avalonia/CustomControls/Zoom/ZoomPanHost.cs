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
    Crop
}
