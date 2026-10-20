using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.DebugTools;
using PicView.Core.Printing;

namespace PicView.Avalonia.Printing;

public static class PrintCore
{
    public static Rect ToRect(this PrintRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    public static Bitmap ToGrayScale(Bitmap src, float dpi)
    {
        ArgumentNullException.ThrowIfNull(src);

        int width, height;
        try
        {
            width = src.PixelSize.Width;
            height = src.PixelSize.Height;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PrintCore), nameof(ToGrayScale), e);
            width = src.PixelSize.Width;
            height = src.PixelSize.Height;
        }
        var stride = width * 4;
        var bufferSize = height * stride;

        var pixelBuffer = IntPtr.Zero;

        try
        {
            pixelBuffer = Marshal.AllocHGlobal(bufferSize);
            var rect = new PixelRect(0, 0, width, height);
            src.CopyPixels(rect, pixelBuffer, bufferSize, stride);

            var managed = new byte[bufferSize];
            Marshal.Copy(pixelBuffer, managed, 0, bufferSize);

            for (var i = 0; i < managed.Length; i += 4)
            {
                var gray = (byte)(0.2126 * managed[i + 2] +
                                  0.7152 * managed[i + 1] +
                                  0.0722 * managed[i + 0]);
                managed[i + 0] = gray;
                managed[i + 1] = gray;
                managed[i + 2] = gray;
            }

            var dst = new WriteableBitmap(
                src.PixelSize,
                new Vector(dpi, dpi),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            using var fb = dst.Lock();
            Marshal.Copy(managed, 0, fb.Address, managed.Length);

            return dst;
        }
        finally
        {
            if (pixelBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pixelBuffer);
            }
        }
    }
}
