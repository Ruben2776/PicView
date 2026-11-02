using Avalonia.Media;
using PicView.Core.Gallery;

namespace PicView.Avalonia.Gallery;

/// <summary>
///     How a thumbnail image fills its item for each <see cref="GalleryStretchMode" />.
/// </summary>
public static class GalleryThumbnailStretch
{
    public static Stretch ToStretch(GalleryStretchMode mode) => mode switch
    {
        GalleryStretchMode.UniformToFill or GalleryStretchMode.FillSquare => Stretch.UniformToFill,
        _ => Stretch.Uniform
    };
}
