using PicView.Core.Sizing;

namespace PicView.Core.Gallery;

public static class GalleryDefaults
{
    public const int DefaultExpandedGalleryHeight = 150;
    public const int DefaultDockedGalleryHeight = 76;
    
    public const int MaxExpandedGalleryItemHeight = 350;
    public const int MinExpandedGalleryItemHeight = 50;
    
    public const int MaxDockedGalleryItemHeight = 150;
    public const int MinDockedGalleryItemHeight = 40;
    
    /// <summary>The smallest gap between gallery items; below it the items look as if they overlap.</summary>
    public const double MinItemSpacing = 5;
    /// <summary>The smallest gap between gallery lines; below it the items look as if they overlap.</summary>
    public const double MinLineSpacing = 8;
    public const double MaxItemSpacing = 70;
    public const double MaxLineSpacing = 70;

    /// <summary>The item spacing that is accepted for <paramref name="value"/>: the nearest one in range.</summary>
    public static double AcceptedItemSpacing(double value) =>
        double.IsNaN(value) ? MinItemSpacing : Math.Clamp(value, MinItemSpacing, MaxItemSpacing);

    /// <summary>The line spacing that is accepted for <paramref name="value"/>: the nearest one in range.</summary>
    public static double AcceptedLineSpacing(double value) =>
        double.IsNaN(value) ? MinLineSpacing : Math.Clamp(value, MinLineSpacing, MaxLineSpacing);

    public const double VeryFastAnimationSpeed = 0.25;
    public const double FastAnimationSpeed = 0.35;
    public const double MediumAnimationSpeed = 0.5;
    public const double SlowAnimationSpeed = 0.6;
    
    public const int BorderTopAndBottomThickness = 2;
    public const int BorderSideThickness = 1;

    /// <summary>The smallest Masonry column width, as a multiple of the item size.</summary>
    public const double MasonryColumnWidthFactor = 4.0 / 3;

    /// <summary>
    /// The smallest width of a Grid cell, as a multiple of the item size, unless the Thumbnail Stretch makes it a
    /// square.
    /// </summary>
    public const double GridCellWidthFactor = 0.9;

    /// <summary>The Rows target height, as a multiple of the item size.</summary>
    public const double RowHeightFactor = 2;

    /// <summary>The lowest width over height a tile takes.</summary>
    public const double MinTileAspectRatio = 1.0 / 3;

    /// <summary>The highest width over height a tile takes.</summary>
    public const double MaxTileAspectRatio = 4;

    /// <summary>
    /// How long the Expanded Gallery's view must rest, with no scroll, resize, item size or display mode change, before
    /// its Masonry and Rows tiles are sharpened.
    /// </summary>
    public const int TileSharpenDelayMilliseconds = 150;

    /// <summary>Width of a gallery docked left or right: items, a side border and the vertical scrollbar.</summary>
    public static double GetDockedGalleryWidth(double itemSize) => itemSize + BorderSideThickness + SizeDefaults.VerticalScrollbarSize;
    /// <summary>Height of a gallery docked top or bottom: items, the top/bottom border and the horizontal scrollbar.</summary>
    public static double GetDockedGalleryHeight(double itemSize) => itemSize + BorderTopAndBottomThickness + SizeDefaults.HorizontalScrollbarSize;
}
