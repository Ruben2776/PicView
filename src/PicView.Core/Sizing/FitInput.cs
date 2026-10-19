using System.Runtime.InteropServices;

namespace PicView.Core.Sizing;

/// <summary>
/// Where the image is fitted: the whole screen, the work area the window grows into with AutoFit,
/// or the window at the size it already has.
/// </summary>
public enum FitMode
{
    FixedWindow,
    AutoFit,
    Fullscreen
}

/// <summary>
/// The parts of the window that are not the image, taken away before the image is fitted.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ReservedSpace(double Top, double Bottom, double GalleryWidth, double GalleryHeight)
{
    /// <param name="tabCount">With more than one tab, the tab strip is shown above the image with the titlebar.</param>
    /// <param name="titlebarHeight">The titlebar's height as it is now.</param>
    /// <param name="isTopToolbarShown">Whether the titlebar is shown.</param>
    /// <param name="showBottomBar">Whether the bottom bar is shown.</param>
    /// <param name="galleryWidth">The width of a gallery docked at the side, or 0.</param>
    /// <param name="galleryHeight">The height of a gallery docked at the top or bottom, or 0.</param>
    public static ReservedSpace For(int tabCount, double titlebarHeight, bool isTopToolbarShown, bool showBottomBar,
        double galleryWidth, double galleryHeight)
    {
        double top;
        if (tabCount > 1)
        {
            top = SizeDefaults.TabHeight + titlebarHeight + 2;
        }
        else if (isTopToolbarShown)
        {
            top = titlebarHeight + 2;
        }
        else
        {
            top = 0;
        }

        var bottom = showBottomBar ? SizeDefaults.BottombarHeight : 0;
        return new ReservedSpace(top, bottom, galleryWidth, galleryHeight);
    }
}

/// <summary>
/// Everything a <see cref="Fit"/> is calculated from.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct FitInput
{
    public required double ImageWidth { get; init; }
    public required double ImageHeight { get; init; }

    /// <summary>
    /// The second image of a side-by-side pair, or 0 when a single image is shown.
    /// </summary>
    public double SecondImageWidth { get; init; }

    /// <inheritdoc cref="SecondImageWidth"/>
    public double SecondImageHeight { get; init; }

    public int QuarterTurns { get; init; }

    public FitMode Mode { get; init; }
    public bool ZoomToFit { get; init; }
    public bool ScrollEnabled { get; init; }

    public ScreenSize Screen { get; init; }

    /// <summary>
    /// The total gap AutoFit keeps free around the window, in device-independent pixels.
    /// </summary>
    public double ScreenMargin { get; init; }

    /// <summary>
    /// The size of the window's content area as it is now, used when the window keeps its size.
    /// </summary>
    public double WindowContentWidth { get; init; }

    /// <inheritdoc cref="WindowContentWidth"/>
    public double WindowContentHeight { get; init; }

    public ReservedSpace ReservedSpace { get; init; }
}
