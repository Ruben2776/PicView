using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Avalonia.ImageHandling;

namespace PicView.Tests.Imaging;

/// <summary>
/// Pixel Info reads a bitmap's pixel as 0xAARRGGBB with straight alpha, whatever the bitmap's own layout.
/// </summary>
public class BitmapPixelReaderTests
{
    [AvaloniaFact]
    public void ReadArgb_BgraPixel_IsItsColor()
    {
        using var bitmap = Bitmap(PixelFormat.Bgra8888, x: 2, y: 1, 0xD5, 0x7B, 0x3A, 0xFF);

        Assert.Equal(0xFF3A7BD5, BitmapPixelReader.ReadArgb(bitmap, 2, 1));
        Assert.Equal(0u, BitmapPixelReader.ReadArgb(bitmap, 0, 0));
    }

    [AvaloniaFact]
    public void ReadArgb_RgbaPixel_IsItsColor()
    {
        using var bitmap = Bitmap(PixelFormat.Rgba8888, x: 0, y: 0, 0x3A, 0x7B, 0xD5, 0xFF);

        Assert.Equal(0xFF3A7BD5, BitmapPixelReader.ReadArgb(bitmap, 0, 0));
    }

    [AvaloniaFact]
    public void ReadArgb_PremultipliedPixel_IsItsStraightColor()
    {
        // Half-transparent white, stored premultiplied
        using var bitmap = Bitmap(PixelFormat.Bgra8888, x: 0, y: 0, 0x80, 0x80, 0x80, 0x80);

        Assert.Equal(0x80FFFFFF, BitmapPixelReader.ReadArgb(bitmap, 0, 0));
    }

    [AvaloniaTheory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(4, 0)]
    [InlineData(0, 2)]
    public void ReadArgb_OffTheBitmap_IsNull(int x, int y)
    {
        using var bitmap = Bitmap(PixelFormat.Bgra8888, x: 0, y: 0, 1, 2, 3, 0xFF);

        Assert.Null(BitmapPixelReader.ReadArgb(bitmap, x, y));
    }

    // A premultiplied 4 by 2 bitmap, clear but for the four bytes of one pixel
    private static WriteableBitmap Bitmap(PixelFormat format, int x, int y, byte b0, byte b1, byte b2, byte b3)
    {
        var bitmap = new WriteableBitmap(new PixelSize(4, 2), new Vector(96, 96), format, AlphaFormat.Premul);
        using var buffer = bitmap.Lock();
        var bytes = new byte[buffer.RowBytes * 2];
        var pixel = y * buffer.RowBytes + x * 4;
        bytes[pixel] = b0;
        bytes[pixel + 1] = b1;
        bytes[pixel + 2] = b2;
        bytes[pixel + 3] = b3;
        Marshal.Copy(bytes, 0, buffer.Address, bytes.Length);
        return bitmap;
    }
}
