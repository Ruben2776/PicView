using System.Runtime.InteropServices;

namespace PicView.Core.Sizing;

[StructLayout(LayoutKind.Auto)]
public readonly record struct Fit(
    double WindowWidth,
    double WindowHeight,
    double Width,
    double Height,
    double ScrollViewerWidth,
    double ScrollViewerHeight,
    double Scale)
{
    /// <summary>
    /// The size of the area the zoomed image is shown in when zoom resizes the window (ADR-0047): the fitted size
    /// times the Zoom Factor, no larger than the Available Area. Like <see cref="Width"/> and <see cref="Height"/>,
    /// it is the size before the image's quarter turns. 0 when the window does not follow the zoom.
    /// </summary>
    public double ViewportWidth { get; init; }

    /// <inheritdoc cref="ViewportWidth"/>
    public double ViewportHeight { get; init; }

    /// <summary>
    /// The Zoom Factor from which the window has stopped growing, since the zoomed image fills the Available Area
    /// both ways. 0 when the window does not follow the zoom.
    /// </summary>
    public double WindowGrowthLimit { get; init; }
}