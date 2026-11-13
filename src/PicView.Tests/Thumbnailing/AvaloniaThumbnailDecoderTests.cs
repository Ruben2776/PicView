using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Avalonia.ImageHandling;

namespace PicView.Tests.Thumbnailing;

public class AvaloniaThumbnailDecoderTests
{
    private readonly AvaloniaThumbnailDecoder _decoder = new(platform: null);

    private static WriteableBitmap Image(int width, int height) =>
        new(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

    [AvaloniaFact]
    public async Task ScaleDown_OffTheUiThread_GivesANewBitmapAtTheHeight()
    {
        using var image = Image(150, 200);

        var scaled = await Task.Run(() => _decoder.ScaleDown(image, 100), TestContext.Current.CancellationToken);

        var bitmap = Assert.IsAssignableFrom<Bitmap>(scaled);
        Assert.NotSame(image, bitmap);
        Assert.Equal(new PixelSize(75, 100), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public async Task ScaleDown_ImageShorterThanTheHeight_KeepsItsSizeInANewBitmap()
    {
        using var image = Image(30, 40);

        var scaled = await Task.Run(() => _decoder.ScaleDown(image, 100), TestContext.Current.CancellationToken);

        var bitmap = Assert.IsAssignableFrom<Bitmap>(scaled);
        Assert.NotSame(image, bitmap);
        Assert.Equal(new PixelSize(30, 40), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public async Task ScaleDown_SolidColor_KeepsTheColor()
    {
        using var image = Image(40, 40);
        using (var framebuffer = image.Lock())
        {
            // Opaque red, in BGRA order
            var pixels = Enumerable.Repeat(new byte[] { 0, 0, 255, 255 }, 40 * 40).SelectMany(p => p).ToArray();
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, framebuffer.Address, pixels.Length);
        }

        var scaled = Assert.IsAssignableFrom<Bitmap>(
            await Task.Run(() => _decoder.ScaleDown(image, 10), TestContext.Current.CancellationToken));

        var center = new byte[4];
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(center, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            scaled.CopyPixels(new PixelRect(5, 5, 1, 1), handle.AddrOfPinnedObject(), 4, 4);
        }
        finally
        {
            handle.Free();
        }

        Assert.Equal(new byte[] { 0, 0, 255, 255 }, center);
    }

    [Fact]
    public void ScaleDown_NotABitmap_GivesNothing()
    {
        Assert.Null(_decoder.ScaleDown(new object(), 100));
    }
}
