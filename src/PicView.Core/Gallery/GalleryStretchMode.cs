namespace PicView.Core.Gallery;

/// <summary>
/// The Thumbnail Stretch of a gallery. The settings file stores these as numbers, so a value keeps its number, and a
/// removed number is never reused.
/// </summary>
public enum GalleryStretchMode
{
    /// <summary>
    /// All borders are stretched to fill the available space, while the image inside maintains the aspect ratio.
    /// </summary>
    Uniform = 0,
    /// <summary>
    /// All borders are stretched to fill the available space, while stretching the image inside to fill the available space.
    /// </summary>
    UniformToFill = 1,
    /// <summary>
    /// Thumbnail is in 1:1 ratio while maintaining the aspect ratio.
    /// </summary>
    Square = 4,
    /// <summary>
    /// Thumbnail is in 1:1 ratio and stretches to fill the available space.
    /// </summary>
    FillSquare = 5,
}
