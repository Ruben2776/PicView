using System.Runtime.InteropServices;

namespace PicView.Core.ImageDecoding;

/// <summary>
/// The pixel size an image file stores, before any EXIF orientation is applied. The default value, 0 by 0, stands
/// for a file that could not be read as an image.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ImageSize(uint Width, uint Height)
{
    /// <summary>
    /// Whether there is no size: the file could not be read as an image.
    /// </summary>
    public bool IsEmpty => Width is 0 || Height is 0;
}
