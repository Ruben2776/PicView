namespace PicView.Avalonia.ImageTransformations;
/// <summary>
/// Zoom changed event arguments.
/// </summary>
public class ZoomChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the zoom ratio for x axis.
    /// </summary>
    public double ZoomX { get; }

    /// <summary>
    /// Gets the zoom ratio for y axis.
    /// </summary>
    public double ZoomY { get; }

    /// <summary>
    /// Gets the pan offset for x axis.
    /// </summary>
    public double OffsetX { get; }

    /// <summary>
    /// Gets the pan offset for y axis.
    /// </summary>
    public double OffsetY { get;  }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomChangedEventArgs"/> class.
    /// </summary>
    /// <param name="zoomX">The zoom ratio for y axis</param>
    /// <param name="zoomY">The zoom ratio for y axis</param>
    /// <param name="offsetX">The pan offset for x axis</param>
    /// <param name="offsetY">The pan offset for y axis</param>
    public ZoomChangedEventArgs(double zoomX, double zoomY, double offsetX, double offsetY)
    {
        ZoomX = zoomX;
        ZoomY = zoomY;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
    
    /// <summary>
    /// Zoom changed event handler.
    /// </summary>
    /// <param name="sender">The sender object.</param>
    /// <param name="e">Zoom changed event arguments.</param>
    public delegate void ZoomChangedEventHandler(object sender, ZoomChangedEventArgs e);
}
