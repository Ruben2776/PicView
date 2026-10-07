#if WINDOWS
using System.Runtime.InteropServices;
using ImageMagick;
using PicView.Core.WindowsNT.Imaging;
using Xunit;

namespace PicView.Tests.Imaging;

public class Win32ImageTests(ITestOutputHelper output)
{
    [Fact]
    public void GetPixelsFromNativeImagingComponent_InvalidOrEmptyPath_ReturnsNull()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var pixels = Win32Image.GetPixelsFromNativeImagingComponent(string.Empty, out var w, out var h);
        Assert.Null(pixels);
        Assert.Equal(0, w);
        Assert.Equal(0, h);

        pixels = Win32Image.GetPixelsFromNativeImagingComponent("non_existent_file.xyz", out w, out h);
        Assert.Null(pixels);
        Assert.Equal(0, w);
        Assert.Equal(0, h);
    }

    [Fact]
    public void GetPixelsFromNativeImagingComponent_ValidPng_DecodesCorrectDimensionsAndPixels()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        try
        {
            using (var img = new MagickImage(MagickColors.Red, 64, 32))
            {
                img.Write(tempFile, MagickFormat.Png);
            }

            var pixels = Win32Image.GetPixelsFromNativeImagingComponent(tempFile, out var width, out var height);
            output.WriteLine($"Pixels null? {pixels == null}, width={width}, height={height}");
            Assert.NotNull(pixels);
            Assert.Equal(64, width);
            Assert.Equal(32, height);
            Assert.Equal(64 * 32 * 4, pixels.Length);

            // Red in BGRA (premultiplied): B=0, G=0, R=255, A=255
            Assert.Equal(0, pixels[0]);     // B
            Assert.Equal(0, pixels[1]);     // G
            Assert.Equal(255, pixels[2]);   // R
            Assert.Equal(255, pixels[3]);   // A
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
#endif
