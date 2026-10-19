using System.Runtime.InteropServices;

namespace PicView.Core.Sizing;

/// <summary>
/// Represents screen dimensions and scaling information.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ScreenSize
{
    /// <summary>
    /// Gets the width of the screen's working area in device-independent pixels.
    /// </summary>
    public double WorkingAreaWidth { get; init; }
    
    /// <summary>
    /// Gets the height of the screen's working area in device-independent pixels.
    /// </summary>
    public double WorkingAreaHeight { get; init; }

    /// <summary>
    /// Gets the width of the screen in device-independent pixels.
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// Gets the height of the screen in device-independent pixels.
    /// </summary>
    public double Height { get; init; }

    /// <summary>
    /// Gets the horizontal position of the screen's bounds in device-independent pixels.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Gets the vertical position of the screen's bounds in device-independent pixels.
    /// </summary>
    public double Y { get; init; }
    
    /// <summary>
    /// Gets the DPI scaling factor of the screen.
    /// </summary>
    public double Scaling { get; init; }
}