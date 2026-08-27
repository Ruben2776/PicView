namespace PicView.Core.Gallery;

public enum GalleryStretchMode
{
    Uniform = 0,
    UniformToFill = 1,
    Fill = 2,
    /// <summary>
    /// No stretching applied
    /// </summary>
    None = 3,
    /// <summary>
    /// Thumbnail is in 1:1 ratio while maintaining the aspect ratio.
    /// </summary>
    Square = 4,
    /// <summary>
    /// Thumbnail is in 1:1 ratio and stretches to fill the available space.
    /// </summary>
    FillSquare = 5,
}
