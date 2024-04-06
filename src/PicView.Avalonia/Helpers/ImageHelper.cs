using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace PicView.Avalonia.Helpers;

public static class ImageHelper
{
    public static WriteableBitmap CreateBitmapFromPixelData(byte[] rgbPixelData, int pixelWidth, int pixelHeight)
    {
        // Standard may need to change on some devices
        var dpi = new Vector(96, 96);

        var bitmap = new WriteableBitmap(
            new PixelSize(pixelWidth, pixelHeight),
            dpi,
            PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        using var frameBuffer = bitmap.Lock();
        Marshal.Copy(rgbPixelData, 0, frameBuffer.Address, rgbPixelData.Length);

        return bitmap;
    }
}