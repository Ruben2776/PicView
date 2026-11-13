using Avalonia;
using Avalonia.Media.Imaging;
using PicView.Core.IPlatform;
using PicView.Core.Thumbnailing;

namespace PicView.Avalonia.ImageHandling;

/// <summary>
/// Makes Avalonia bitmaps for <see cref="Thumbnails"/>: the shell's thumbnails through the platform service, and
/// ImageMagick, Skia and the SVG fallback for the rest.
/// </summary>
public sealed class AvaloniaThumbnailDecoder(IPlatformSpecificService? platform) : IThumbnailDecoder
{
    public object? GetReadyMade(FileInfo file, uint height) => GetThumbnails.GetReadyMade(file, height, platform);

    public object? Decode(FileInfo file, uint height) => GetThumbnails.Decode(file, height, platform);

    public object? ScaleDown(object image, uint height)
    {
        if (image is not Bitmap bitmap || height is 0)
        {
            return null;
        }

        var size = bitmap.PixelSize;
        if (size.Width <= 0 || size.Height <= 0)
        {
            return null;
        }

        // Never taller than the image itself, and always a new bitmap, since the image belongs to the image cache
        var targetHeight = (int)Math.Min(height, (uint)size.Height);
        var targetWidth = Math.Max(1, (int)Math.Round(size.Width * (double)targetHeight / size.Height));
        return bitmap.CreateScaledBitmap(new PixelSize(targetWidth, targetHeight), BitmapInterpolationMode.HighQuality);
    }
}
