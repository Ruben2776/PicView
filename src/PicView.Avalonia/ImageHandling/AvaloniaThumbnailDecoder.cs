using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Thumbnailing;
using SkiaSharp;

namespace PicView.Avalonia.ImageHandling;

/// <summary>
/// Makes Avalonia bitmaps for <see cref="Thumbnails"/>: the shell's thumbnails through the platform service, and
/// ImageMagick, Skia and the SVG fallback for the rest.
/// </summary>
public sealed class AvaloniaThumbnailDecoder(IPlatformSpecificService? platform) : IThumbnailDecoder
{
    public object? GetReadyMade(FileInfo file, uint height) => GetThumbnails.GetReadyMade(file, height, platform);

    public object? Decode(FileInfo file, uint height) => GetThumbnails.Decode(file, height, platform);

    /// <remarks>
    /// Scales with Skia rather than <see cref="Bitmap.CreateScaledBitmap"/>, which takes only immutable bitmaps and
    /// throws for the <see cref="WriteableBitmap"/>s PicView decodes into.
    /// </remarks>
    public object? ScaleDown(object image, uint height)
    {
        if (image is not Bitmap bitmap || height is 0)
        {
            return null;
        }

        var size = bitmap.PixelSize;
        if (size.Width <= 0 || size.Height <= 0 || ColorTypeOf(bitmap.Format) is not { } colorType)
        {
            return null;
        }

        // Never taller than the image itself, and always a new bitmap, since the image belongs to the image cache
        var targetHeight = (int)Math.Min(height, (uint)size.Height);
        var targetWidth = Math.Max(1, (int)Math.Round(size.Width * (double)targetHeight / size.Height));
        WriteableBitmap? scaled = null;
        try
        {
            var alphaType = bitmap.AlphaFormat is AlphaFormat.Unpremul ? SKAlphaType.Unpremul : SKAlphaType.Premul;
            using var source = new SKBitmap(new SKImageInfo(size.Width, size.Height, colorType, alphaType));
            bitmap.CopyPixels(new PixelRect(size), source.GetPixels(), source.ByteCount, source.RowBytes);

            scaled = new WriteableBitmap(new PixelSize(targetWidth, targetHeight), bitmap.Dpi, PixelFormat.Bgra8888,
                AlphaFormat.Premul);
            using var framebuffer = scaled.Lock();
            using var destination = new SKPixmap(
                new SKImageInfo(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
                framebuffer.Address, framebuffer.RowBytes);
            if (source.ScalePixels(destination, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)))
            {
                return scaled;
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(AvaloniaThumbnailDecoder), nameof(ScaleDown), e);
        }

        // The file is decoded instead
        scaled?.Dispose();
        return null;
    }

    private static SKColorType? ColorTypeOf(PixelFormat? format)
    {
        if (format == PixelFormat.Bgra8888)
        {
            return SKColorType.Bgra8888;
        }

        if (format == PixelFormat.Rgba8888)
        {
            return SKColorType.Rgba8888;
        }

        return null;
    }
}
