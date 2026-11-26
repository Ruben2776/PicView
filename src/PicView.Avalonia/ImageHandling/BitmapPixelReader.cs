using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.ImageHandling;

/// <summary>
/// Reads one pixel of a bitmap, for Pixel Info.
/// </summary>
public static class BitmapPixelReader
{
    /// <summary>
    /// The pixel at <paramref name="x"/>, <paramref name="y"/> as 0xAARRGGBB with straight alpha, or null when the
    /// point is off the bitmap, its format is not 32 bits a pixel, or it cannot be read.
    /// </summary>
    public static unsafe uint? ReadArgb(Bitmap bitmap, int x, int y)
    {
        var format = bitmap.Format;
        var isBgra = format == PixelFormat.Bgra8888;
        if (!isBgra && format != PixelFormat.Rgba8888)
        {
            return null;
        }

        var pixel = stackalloc byte[4];
        try
        {
            var size = bitmap.PixelSize;
            if (x < 0 || y < 0 || x >= size.Width || y >= size.Height)
            {
                return null;
            }

            bitmap.CopyPixels(new PixelRect(x, y, 1, 1), (nint)pixel, 4, 4);
        }
        catch (Exception e)
        {
            // The image cache may dispose the bitmap while the pointer is still over it
            DebugHelper.LogDebug(nameof(BitmapPixelReader), nameof(ReadArgb), e);
            return null;
        }

        uint red = isBgra ? pixel[2] : pixel[0];
        uint green = pixel[1];
        uint blue = isBgra ? pixel[0] : pixel[2];
        uint alpha = pixel[3];
        switch (bitmap.AlphaFormat)
        {
            case AlphaFormat.Opaque:
                alpha = 0xFF;
                break;
            case AlphaFormat.Premul when alpha is > 0 and < 0xFF:
                red = Straight(red, alpha);
                green = Straight(green, alpha);
                blue = Straight(blue, alpha);
                break;
        }

        return alpha << 24 | red << 16 | green << 8 | blue;
    }

    // A premultiplied channel back to its straight value, rounded
    private static uint Straight(uint channel, uint alpha) => Math.Min(0xFF, (channel * 0xFF + alpha / 2) / alpha);
}
