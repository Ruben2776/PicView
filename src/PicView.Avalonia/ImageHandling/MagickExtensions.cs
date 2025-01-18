using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;

namespace PicView.Avalonia.ImageHandling;

public static class MagickExtensions
{
    public static WriteableBitmap ToWriteableBitmap<TQuantumType>(this IMagickImage<TQuantumType> self)
        where TQuantumType : struct, IConvertible
    {
        var size = new PixelSize((int)self.Width, (int)self.Height);
        var dpi = new Vector(96, 96);
        var bitmap = new WriteableBitmap(size, dpi, PixelFormats.Rgba8888, AlphaFormat.Unpremul);

        using var framebuffer = bitmap.Lock();
        using var pixels = self.GetPixelsUnsafe();
        const string mapping = "RGBA";

        var destination = framebuffer.Address;
        for (var y = 0; y < self.Height; y++)
        {
            var bytes = pixels.ToByteArray(0, y, self.Width, 1, mapping);
            if (bytes != null)
                Marshal.Copy(bytes, 0, destination, bytes.Length);

            destination += framebuffer.RowBytes;
        }

        return bitmap;
    }
}
