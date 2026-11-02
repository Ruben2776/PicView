namespace PicView.Core.Gallery;

public enum GalleryOrientation
{
    Horizontal,
    Vertical
}

public readonly record struct GalleryInsets(double Left, double Top, double Right, double Bottom)
{
    public static GalleryInsets Zero => new(0, 0, 0, 0);
}

/// <summary>
///     The settings a gallery's appearance depends on, passed in so <see cref="GalleryAppearance.For" /> stays pure.
/// </summary>
public readonly record struct GalleryAppearanceInputs(
    double DockedItemSize,
    double ExpandedItemSize,
    GalleryStretchMode DockedStretch,
    GalleryStretchMode ExpandedStretch,
    double ItemSpacing,
    bool ShowBottomNavBar,
    bool ShowHoverNavigationBar);

/// <summary>
///     How a gallery looks in a given mode and dock position. <see cref="Extent" /> is the docked thickness,
///     <see cref="double.NaN" /> when the gallery fills its parent, and 0 when closed.
/// </summary>
public readonly record struct GalleryAppearance(
    GalleryMode Mode,
    GalleryDockPosition Dock,
    bool IsShown,
    GalleryOrientation Orientation,
    bool ScrollsHorizontally,
    bool Wraps,
    double ItemHeight,
    double ItemWidth,
    GalleryStretchMode Stretch,
    double ItemSpacing,
    GalleryInsets Margin,
    GalleryInsets Border,
    double Extent,
    bool ShowsHoverBar)
{
    public static GalleryAppearance For(GalleryMode shownMode, GalleryDockPosition dock, GalleryAppearanceInputs inputs)
    {
        switch (shownMode)
        {
            case GalleryMode.Docked:
                var horizontal = dock is GalleryDockPosition.Top or GalleryDockPosition.Bottom;
                var border = dock switch
                {
                    GalleryDockPosition.Top => new GalleryInsets(0, 0, 0, 1),
                    GalleryDockPosition.Bottom => new GalleryInsets(0, 1, 0, 0),
                    GalleryDockPosition.Left => new GalleryInsets(0, 0, 1, 0),
                    _ => new GalleryInsets(1, 0, 0, 0)
                };
                return new GalleryAppearance(
                    shownMode, dock, true,
                    horizontal ? GalleryOrientation.Horizontal : GalleryOrientation.Vertical,
                    horizontal,
                    false,
                    inputs.DockedItemSize,
                    ItemWidthFor(inputs.DockedStretch, inputs.DockedItemSize),
                    inputs.DockedStretch,
                    0,
                    GalleryInsets.Zero,
                    border,
                    horizontal
                        ? GalleryDefaults.GetDockedGalleryHeight(inputs.DockedItemSize)
                        : GalleryDefaults.GetDockedGalleryWidth(inputs.DockedItemSize),
                    !inputs.ShowBottomNavBar && inputs.ShowHoverNavigationBar);
            case GalleryMode.Expanded:
                return new GalleryAppearance(
                    shownMode, dock, true,
                    GalleryOrientation.Vertical,
                    true,
                    true,
                    inputs.ExpandedItemSize,
                    ItemWidthFor(inputs.ExpandedStretch, inputs.ExpandedItemSize),
                    inputs.ExpandedStretch,
                    inputs.ItemSpacing,
                    new GalleryInsets(15, 40, 15, 5),
                    GalleryInsets.Zero,
                    double.NaN,
                    false);
            default:
                return new GalleryAppearance(
                    shownMode, dock, false,
                    GalleryOrientation.Vertical,
                    false,
                    false,
                    0,
                    double.NaN,
                    inputs.DockedStretch,
                    0,
                    GalleryInsets.Zero,
                    GalleryInsets.Zero,
                    0,
                    false);
        }
    }

    private static double ItemWidthFor(GalleryStretchMode stretch, double itemHeight)
    {
        return stretch is GalleryStretchMode.Square or GalleryStretchMode.FillSquare ? itemHeight : double.NaN;
    }
}
