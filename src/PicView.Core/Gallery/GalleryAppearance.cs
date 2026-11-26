using System.Runtime.InteropServices;

namespace PicView.Core.Gallery;

public enum GalleryOrientation
{
    Horizontal,
    Vertical
}

[StructLayout(LayoutKind.Auto)]
public readonly record struct GalleryInsets(double Left, double Top, double Right, double Bottom)
{
    public static GalleryInsets Zero => new(0, 0, 0, 0);
}

/// <summary>
///     The settings a gallery's appearance depends on, passed in so <see cref="GalleryAppearance.For" /> stays pure.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct GalleryAppearanceInputs(
    double DockedItemSize,
    double ExpandedItemSize,
    GalleryStretchMode DockedStretch,
    GalleryStretchMode ExpandedStretch,
    double ItemSpacing,
    GalleryDisplayMode ExpandedDisplayMode = GalleryDisplayMode.Grid);

/// <summary>
///     How a gallery looks in a given mode and dock position. <see cref="Extent" /> is the docked thickness,
///     <see cref="double.NaN" /> when the gallery fills its parent, and 0 when closed.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct GalleryAppearance(
    GalleryMode Mode,
    GalleryDockPosition Dock,
    bool IsShown,
    GalleryOrientation Orientation,
    bool Wraps,
    double ItemHeight,
    double ItemWidth,
    GalleryStretchMode Stretch,
    double ItemSpacing,
    GalleryInsets Margin,
    GalleryInsets Border,
    double Extent)
{
    /// <summary>
    ///     The thickness the gallery has when docked at <see cref="Dock" />: <see cref="Extent" /> when docked, and for an
    ///     Expanded Gallery the thickness it gets back if it returns to docked. 0 when closed.
    /// </summary>
    public double DockedExtent { get; init; }

    /// <summary>
    ///     Whether the gallery scrolls sideways, which only a gallery docked at the top or the bottom does. The Expanded
    ///     Gallery scrolls down in every display mode.
    /// </summary>
    public bool ScrollsHorizontally => Orientation is GalleryOrientation.Horizontal;

    /// <summary>
    ///     How the gallery arranges its items. Grid unless the gallery is Expanded, so a Docked or Closed record is
    ///     equal in every display mode and nothing is re-applied to it.
    /// </summary>
    public GalleryDisplayMode DisplayMode { get; init; }

    public static GalleryAppearance For(GalleryMode shownMode, GalleryDockPosition dock, GalleryAppearanceInputs inputs)
    {
        switch (shownMode)
        {
            case GalleryMode.Docked:
                var horizontal = dock is GalleryDockPosition.Top or GalleryDockPosition.Bottom;
                var dockedExtent = DockedExtentFor(dock, inputs.DockedItemSize);
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
                    false,
                    inputs.DockedItemSize,
                    ItemWidthFor(inputs.DockedStretch, inputs.DockedItemSize),
                    inputs.DockedStretch,
                    0,
                    GalleryInsets.Zero,
                    border,
                    dockedExtent)
                {
                    DockedExtent = dockedExtent
                };
            case GalleryMode.Expanded:
                var displayMode = GalleryDisplayModes.Validate(inputs.ExpandedDisplayMode);
                var tiles = GalleryDisplayModes.ShowsTiles(displayMode);
                return new GalleryAppearance(
                    shownMode, dock, true,
                    GalleryOrientation.Vertical,
                    true,
                    inputs.ExpandedItemSize,
                    tiles ? double.NaN : ItemWidthFor(inputs.ExpandedStretch, inputs.ExpandedItemSize),
                    tiles ? GalleryStretchMode.UniformToFill : inputs.ExpandedStretch,
                    inputs.ItemSpacing,
                    new GalleryInsets(15, 40, 15, 5),
                    GalleryInsets.Zero,
                    double.NaN)
                {
                    DockedExtent = DockedExtentFor(dock, inputs.DockedItemSize),
                    DisplayMode = displayMode
                };
            default:
                return new GalleryAppearance(
                    shownMode, dock, false,
                    GalleryOrientation.Vertical,
                    false,
                    0,
                    double.NaN,
                    inputs.DockedStretch,
                    0,
                    GalleryInsets.Zero,
                    GalleryInsets.Zero,
                    0);
        }
    }

    private static double DockedExtentFor(GalleryDockPosition dock, double dockedItemSize) =>
        dock is GalleryDockPosition.Top or GalleryDockPosition.Bottom
            ? GalleryDefaults.GetDockedGalleryHeight(dockedItemSize)
            : GalleryDefaults.GetDockedGalleryWidth(dockedItemSize);

    private static double ItemWidthFor(GalleryStretchMode stretch, double itemHeight)
    {
        return stretch is GalleryStretchMode.Square or GalleryStretchMode.FillSquare ? itemHeight : double.NaN;
    }
}
