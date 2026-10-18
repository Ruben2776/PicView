using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;

namespace PicView.Avalonia.ImageHandling;

/// <summary>
/// Copies Magick pixels straight into a <see cref="WriteableBitmap"/> framebuffer as premultiplied BGRA,
/// which is Skia's native format. This avoids the managed byte[] per row that
/// <c>ToWriteableBitmap</c> allocates, and the unpremultiplied RGBA conversion Skia does when drawing.
/// </summary>
internal static class MagickBitmapConverter
{
    public static WriteableBitmap ToBgraWriteableBitmap(IMagickImage<byte> magickImage)
    {
        if (!CanCopyDirectly(magickImage))
        {
            return magickImage.ToWriteableBitmap();
        }

        using var pixels = magickImage.GetPixelsUnsafe();
        var red = pixels.GetChannelIndex(PixelChannel.Red);
        if (red is null)
        {
            return magickImage.ToWriteableBitmap();
        }

        // Gray images only have one color channel, which Magick reports as red
        var green = pixels.GetChannelIndex(PixelChannel.Green) ?? red.Value;
        var blue = pixels.GetChannelIndex(PixelChannel.Blue) ?? red.Value;
        var alpha = pixels.GetChannelIndex(PixelChannel.Alpha);

        var width = magickImage.Width;
        var height = (int)magickImage.Height;
        var bitmap = new WriteableBitmap(new PixelSize((int)width, height), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Premul);

        using var framebuffer = bitmap.Lock();
        for (var y = 0; y < height; y++)
        {
            var source = pixels.GetAreaPointer(0, y, width, 1);
            if (source == IntPtr.Zero)
            {
                bitmap.Dispose();
                return magickImage.ToWriteableBitmap();
            }

            CopyRow(source, framebuffer.Address + y * framebuffer.RowBytes, (int)width,
                (int)magickImage.ChannelCount, (int)red.Value, (int)green, (int)blue, (int?)alpha);
        }

        return bitmap;
    }

    private static bool CanCopyDirectly(IMagickImage<byte> magickImage) =>
        magickImage.ColorSpace is ColorSpace.sRGB or ColorSpace.RGB or ColorSpace.Gray or ColorSpace.LinearGray
        && magickImage.Width > 0 && magickImage.Height > 0;

    private static unsafe void CopyRow(IntPtr sourceRow, IntPtr destinationRow, int width, int channels,
        int red, int green, int blue, int? alpha)
    {
        var source = (byte*)sourceRow;
        var destination = (byte*)destinationRow;

        if (alpha is null)
        {
            for (var x = 0; x < width; x++, source += channels, destination += 4)
            {
                destination[0] = source[blue];
                destination[1] = source[green];
                destination[2] = source[red];
                destination[3] = byte.MaxValue;
            }

            return;
        }

        var alphaIndex = alpha.Value;
        for (var x = 0; x < width; x++, source += channels, destination += 4)
        {
            var a = source[alphaIndex];
            if (a == byte.MaxValue)
            {
                destination[0] = source[blue];
                destination[1] = source[green];
                destination[2] = source[red];
            }
            else
            {
                destination[0] = Premultiply(source[blue], a);
                destination[1] = Premultiply(source[green], a);
                destination[2] = Premultiply(source[red], a);
            }

            destination[3] = a;
        }
    }

    private static byte Premultiply(byte color, byte alpha) => (byte)((color * alpha + 127) / 255);
}
