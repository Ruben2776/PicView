using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using PicView.Avalonia.ImageHandling;

namespace PicView.Tests.Imaging;

public class GetImageTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"PicView.GetImageTests.{Guid.NewGuid():N}");

    public GetImageTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [AvaloniaTheory]
    [InlineData("png")]
    [InlineData("jpg")]
    [InlineData("webp")]
    public void GetSkBitmap_CommonFormat_MatchesAvaloniaStreamDecode(string extension)
    {
        var path = CreateGradientImage(extension, withAlpha: extension != "jpg");

        using var expected = new Bitmap(path);
        using var actual = GetImage.GetSkBitmap(new FileInfo(path));

        Assert.NotNull(actual);
        Assert.Equal(expected.PixelSize, actual.PixelSize);
        Assert.Equal(ReadPremulBgra(expected), ReadPremulBgra(actual));
    }

    [AvaloniaFact]
    public void GetSkBitmap_UnrecognizedFile_FallsBackToStreamDecodeAndThrows()
    {
        var path = Path.Combine(_directory, "not-an-image.png");
        File.WriteAllText(path, "not an image");

        Assert.ThrowsAny<Exception>(() => GetImage.GetSkBitmap(new FileInfo(path)));
    }

    [AvaloniaTheory]
    [InlineData(ColorType.TrueColorAlpha)]
    [InlineData(ColorType.TrueColor)]
    [InlineData(ColorType.Grayscale)]
    [InlineData(ColorType.GrayscaleAlpha)]
    public async Task GetNonStandardBitmapAsync_ColorType_MatchesMagickToWriteableBitmap(ColorType colorType)
    {
        var path = CreateGradientImage("png", withAlpha: true);
        using (var image = new MagickImage(path))
        {
            image.ColorType = colorType;
            path = Path.Combine(_directory, $"{colorType}.tif");
            image.Write(path, MagickFormat.Tiff);
        }

        using var magickImage = new MagickImage(path);
        using var expected = magickImage.ToWriteableBitmap();
        using var actual = await GetImage.GetNonStandardBitmapAsync(new FileInfo(path), new MagickImage());

        Assert.NotNull(actual);
        // ToWriteableBitmap produces Rgba8888, so Bgra8888 shows the direct framebuffer copy was used
        Assert.Equal(PixelFormat.Bgra8888, actual.Format);
        Assert.Equal(expected.PixelSize, actual.PixelSize);
        Assert.Equal(ReadPremulBgra(expected), ReadPremulBgra(actual));
    }

    [AvaloniaTheory]
    [InlineData("jpg", 150u)] // JPEG decodes 1/4 size straight into the target
    [InlineData("jpg", 100u)] // JPEG decodes 1/4 size, then resizes
    [InlineData("png", 100u)]
    [InlineData("png", 900u)] // Upscale
    public async Task GetThumbAsync_CommonFormat_MatchesDecodeToHeight(string extension, uint height)
    {
        var path = CreateGradientImage(extension, withAlpha: extension != "jpg", width: 800, height: 600);

        using var stream = File.OpenRead(path);
        using var expected = Bitmap.DecodeToHeight(stream, (int)height);
        using var actual = await GetThumbnails.GetThumbAsync(new FileInfo(path), height);

        Assert.NotNull(actual);
        Assert.Equal(expected.PixelSize, actual.PixelSize);
        Assert.Equal(ReadPremulBgra(expected), ReadPremulBgra(actual));
    }

    [AvaloniaFact]
    public async Task GetThumbAsync_MagickFormat_MatchesMagickToWriteableBitmap()
    {
        var path = CreateGradientImage("png", withAlpha: true, width: 800, height: 600);
        using (var image = new MagickImage(path))
        {
            path = Path.Combine(_directory, "gradient.qoi");
            image.Write(path, MagickFormat.Qoi);
        }

        using var magickImage = new MagickImage(path);
        magickImage.AutoOrient();
        magickImage.Thumbnail(new MagickGeometry(0, 100));
        using var expected = magickImage.ToWriteableBitmap();
        using var actual = await GetThumbnails.GetThumbAsync(new FileInfo(path), 100);

        Assert.NotNull(actual);
        Assert.Equal(PixelFormat.Bgra8888, actual.Format);
        Assert.Equal(expected.PixelSize, actual.PixelSize);
        Assert.Equal(ReadPremulBgra(expected), ReadPremulBgra(actual));
    }

    [AvaloniaTheory]
    [InlineData("tif", MagickFormat.Tiff)]
    [InlineData("TIF", MagickFormat.Tiff)]
    [InlineData("tga", MagickFormat.Tga)]
    public async Task GetThumbAsync_FormatSkiaCannotDecode_ReturnsThumbnail(string extension, MagickFormat format)
    {
        var path = CreateGradientImage("png", withAlpha: true, width: 800, height: 600);
        using (var image = new MagickImage(path))
        {
            path = Path.Combine(_directory, $"gradient.{extension}");
            image.Write(path, format);
        }

        using var actual = await GetThumbnails.GetThumbAsync(new FileInfo(path), 100);

        Assert.NotNull(actual);
        Assert.Equal(new PixelSize(133, 100), actual.PixelSize);
    }

    private string CreateGradientImage(string extension, bool withAlpha, int width = 61, int height = 37)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                pixels[i] = (byte)(x * 4);
                pixels[i + 1] = (byte)(y * 6);
                pixels[i + 2] = (byte)((x + y) * 2);
                pixels[i + 3] = withAlpha ? (byte)((x * 7 + y * 3) % 256) : byte.MaxValue;
            }
        }

        using var image = new MagickImage();
        image.ReadPixels(pixels, new PixelReadSettings((uint)width, (uint)height, StorageType.Char, PixelMapping.RGBA));
        if (extension == "webp")
        {
            image.Settings.SetDefine(MagickFormat.WebP, "lossless", true);
        }

        var path = Path.Combine(_directory, $"gradient.{extension}");
        image.Write(path);
        return path;
    }

    /// <summary>
    /// Reads any bitmap as premultiplied BGRA, so bitmaps stored in different formats can be compared.
    /// </summary>
    private static byte[] ReadPremulBgra(Bitmap bitmap)
    {
        var size = bitmap.PixelSize;
        var stride = size.Width * 4;
        var pixels = new byte[stride * size.Height];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), pixels.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        var isRgba = bitmap.Format == PixelFormat.Rgba8888;
        Assert.True(isRgba || bitmap.Format == PixelFormat.Bgra8888, $"Unexpected format {bitmap.Format}");
        var isUnpremul = bitmap.AlphaFormat == AlphaFormat.Unpremul;

        for (var i = 0; i < pixels.Length; i += 4)
        {
            if (isRgba)
            {
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
            }

            if (isUnpremul)
            {
                var a = pixels[i + 3];
                pixels[i] = (byte)((pixels[i] * a + 127) / 255);
                pixels[i + 1] = (byte)((pixels[i + 1] * a + 127) / 255);
                pixels[i + 2] = (byte)((pixels[i + 2] * a + 127) / 255);
            }
        }

        return pixels;
    }
}
