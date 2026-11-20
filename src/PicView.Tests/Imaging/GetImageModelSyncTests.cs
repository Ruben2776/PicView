using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.Imaging;

/// <summary>
/// <see cref="GetImageModel.GetImageModelSync"/> must build the same model as <see cref="GetImageModel.GetImageModelAsync"/>.
/// </summary>
public class GetImageModelSyncTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"PicView.GetImageModelSyncTests.{Guid.NewGuid():N}");

    public GetImageModelSyncTests()
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
    [InlineData("gif")]
    [InlineData("bmp")]
    [InlineData("tif")]
    [InlineData("tga")]
    public async Task GetImageModelSync_Format_MatchesAsync(string extension)
    {
        var file = CreateImage(extension);

        await AssertSameModelAsync(file);
    }

    [AvaloniaFact]
    public async Task GetImageModelSync_RotatedJpeg_MatchesAsync()
    {
        var file = CreateImage("jpg", static image => image.Orientation = Orientation.RightTop);

        await AssertSameModelAsync(file);
    }

    [AvaloniaFact]
    public async Task GetImageModelSync_EightBitGrayPng_MatchesAsync()
    {
        var file = CreateImage("png", static image =>
        {
            image.ColorType = ColorType.Grayscale;
            image.Depth = 8;
        });

        await AssertSameModelAsync(file);
    }

    [AvaloniaFact]
    public async Task GetImageModelSync_AnimatedGif_MatchesAsync()
    {
        var path = Path.Combine(_directory, "animated.gif");
        using (var frames = new MagickImageCollection())
        {
            frames.Add(new MagickImage(MagickColors.Red, 8, 8) { AnimationDelay = 10 });
            frames.Add(new MagickImage(MagickColors.Blue, 8, 8) { AnimationDelay = 10 });
            frames.Write(path, MagickFormat.Gif);
        }

        Assert.Equal(ImageType.AnimatedGif, await AssertSameModelAsync(new FileInfo(path)));
    }

    [AvaloniaFact]
    public async Task GetImageModelSync_Base64File_MatchesAsync()
    {
        var png = CreateImage("png");
        var path = Path.Combine(_directory, "image.b64");
        await File.WriteAllTextAsync(path, Convert.ToBase64String(await File.ReadAllBytesAsync(png.FullName,
            TestContext.Current.CancellationToken)), TestContext.Current.CancellationToken);

        Assert.Equal(ImageType.Bitmap, await AssertSameModelAsync(new FileInfo(path)));
    }

    [AvaloniaFact]
    public async Task GetImageModelSync_NotAnImage_MatchesAsync()
    {
        var path = Path.Combine(_directory, "not-an-image.png");
        await File.WriteAllTextAsync(path, "not an image", TestContext.Current.CancellationToken);

        Assert.Equal(ImageType.Invalid, await AssertSameModelAsync(new FileInfo(path)));
    }

    [Fact]
    public void GetImageModelSync_NullFile_ReturnsInvalidModel()
    {
        using var model = GetImageModel.GetImageModelSync(null!);

        Assert.Equal(ImageType.Invalid, model.ImageType);
        Assert.Null(model.Image);
    }

    /// <summary>Loads <paramref name="file"/> both ways, compares the models and returns their image type.</summary>
    private static async Task<ImageType> AssertSameModelAsync(FileInfo file)
    {
        using var expected = await GetImageModel.GetImageModelAsync(file);
        using var actual = GetImageModel.GetImageModelSync(file);

        Assert.Equal(expected.ImageType, actual.ImageType);
        Assert.Equal(expected.PixelWidth, actual.PixelWidth);
        Assert.Equal(expected.PixelHeight, actual.PixelHeight);
        Assert.Equal(expected.FileInfo?.FullName, actual.FileInfo?.FullName);
        Assert.Equal(expected.TiffNavigation?.PageCount, actual.TiffNavigation?.PageCount);
        Assert.Equal(expected.Image?.GetType(), actual.Image?.GetType());

        if (expected.Image is Bitmap expectedBitmap && actual.Image is Bitmap actualBitmap)
        {
            Assert.Equal(expectedBitmap.Format, actualBitmap.Format);
            Assert.Equal(ReadPixels(expectedBitmap), ReadPixels(actualBitmap));
        }

        return actual.ImageType;
    }

    private FileInfo CreateImage(string extension, Action<MagickImage>? configure = null)
    {
        const int width = 61;
        const int height = 37;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                pixels[i] = (byte)(x * 4);
                pixels[i + 1] = (byte)(y * 6);
                pixels[i + 2] = (byte)((x + y) * 2);
                pixels[i + 3] = byte.MaxValue;
            }
        }

        using var image = new MagickImage();
        image.ReadPixels(pixels, new PixelReadSettings(width, height, StorageType.Char, PixelMapping.RGBA));
        configure?.Invoke(image);

        var path = Path.Combine(_directory, $"image-{Guid.NewGuid():N}.{extension}");
        image.Write(path);
        return new FileInfo(path);
    }

    private static byte[] ReadPixels(Bitmap bitmap)
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

        return pixels;
    }
}
