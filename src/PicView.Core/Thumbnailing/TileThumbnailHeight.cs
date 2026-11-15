namespace PicView.Core.Thumbnailing;

/// <summary>
/// How tall a thumbnail must be to cover a gallery tile without being scaled up.
/// </summary>
public static class TileThumbnailHeight
{
    /// <summary>
    /// The smallest height, in pixels, at which an image of the given upright size covers a tile of the given size in
    /// device pixels. Never taller than the image itself and never zero. An image with a side of 0 is treated as a
    /// square and not capped.
    /// </summary>
    public static uint For(double tileWidth, double tileHeight, uint imagePixelWidth, uint imagePixelHeight)
    {
        var known = imagePixelWidth > 0 && imagePixelHeight > 0;
        var aspectRatio = known ? (double)imagePixelWidth / imagePixelHeight : 1d;

        var needed = Math.Ceiling(Math.Max(tileHeight, tileWidth / aspectRatio));
        if (known)
        {
            needed = Math.Min(needed, imagePixelHeight);
        }

        return (uint)Math.Max(needed, 1d);
    }
}
