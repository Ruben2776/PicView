using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Core.Models;
using PicView.Core.Navigation;

namespace PicView.Tests.Imaging;

public class GetOriginalImageTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"PicView.GetOriginalImageTests.{Guid.NewGuid():N}");

    private readonly SharedImageCache _cache = new(f => new ValueTask<ImageModel>(new ImageModel { FileInfo = f }));

    public GetOriginalImageTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task GetBitmapAsync_Cached_ReturnsTheCachedBitmap()
    {
        var file = new FileInfo(Path.Combine(_directory, "not-on-disk.png"));
        var cached = new WriteableBitmap(new PixelSize(40, 30), new Vector(96, 96), PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        _cache.Put(1, file, new ImageModel { FileInfo = file, Image = cached });

        var bitmap = await GetOriginalImage.GetBitmapAsync(file, _cache);

        Assert.Same(cached, bitmap);
    }

    [AvaloniaFact]
    public async Task GetBitmapAsync_NotCached_DecodesTheFullImage()
    {
        var path = Path.Combine(_directory, "image.png");
        using (var image = new MagickImage(MagickColors.Red, 64, 48))
        {
            await image.WriteAsync(path, MagickFormat.Png);
        }

        using var bitmap = await GetOriginalImage.GetBitmapAsync(new FileInfo(path), _cache);

        Assert.NotNull(bitmap);
        Assert.Equal(new PixelSize(64, 48), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public async Task GetBitmapAsync_ExifRotated_ReturnsItUpright()
    {
        var path = Path.Combine(_directory, "rotated.jpg");
        using (var image = new MagickImage(MagickColors.Red, 64, 48))
        {
            // Rotate 90° clockwise to display. Magick writes the tag only with both an EXIF profile and the property set
            var exif = new ExifProfile();
            exif.SetValue(ExifTag.Orientation, (ushort)6);
            image.SetProfile(exif);
            image.Orientation = Orientation.RightTop;
            await image.WriteAsync(path, MagickFormat.Jpeg);
        }

        using var bitmap = await GetOriginalImage.GetBitmapAsync(new FileInfo(path), _cache);

        Assert.NotNull(bitmap);
        Assert.Equal(new PixelSize(48, 64), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public async Task GetBitmapAsync_Svg_ReturnsNull()
    {
        var path = Path.Combine(_directory, "vector.svg");
        await File.WriteAllTextAsync(path,
            """<svg xmlns="http://www.w3.org/2000/svg" width="10" height="10"><rect width="10" height="10"/></svg>""");

        var bitmap = await GetOriginalImage.GetBitmapAsync(new FileInfo(path), _cache);

        Assert.Null(bitmap);
    }

    [AvaloniaFact]
    public async Task GetBitmapAsync_NotAnImage_ReturnsNull()
    {
        var path = Path.Combine(_directory, "broken.png");
        await File.WriteAllTextAsync(path, "not an image");

        var bitmap = await GetOriginalImage.GetBitmapAsync(new FileInfo(path), _cache);

        Assert.Null(bitmap);
    }
}
