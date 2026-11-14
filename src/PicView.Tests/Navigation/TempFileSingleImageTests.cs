using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Navigation;
using PicView.Core.ImageDecoding;
using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class TempFileSingleImageTests : IDisposable
{
    private const string Viewport = "width='40' height='20'";

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"PicView.TempFileSingleImageTests.{Guid.NewGuid():N}");

    public TempFileSingleImageTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task LoadAsync_SupportedSvg_ReturnsDrawingModelWithoutFileInfo()
    {
        using var model = await TempFileSingleImage.LoadAsync(Write("a.svg",
            Document("<rect x='5' y='5' width='10' height='10' fill='red'/>", Viewport)));

        Assert.NotNull(model);
        Assert.IsType<SvgDrawing>(model.Image);
        Assert.Equal(ImageType.Svg, model.ImageType);
        Assert.Equal(40u, model.PixelWidth);
        Assert.Equal(20u, model.PixelHeight);
        Assert.Null(model.FileInfo);
    }

    [AvaloniaFact]
    public async Task LoadAsync_UnsupportedSvg_ReturnsFallbackBitmapAtTheSvgSize()
    {
        using var model = await TempFileSingleImage.LoadAsync(Write("b.svg",
            Document("<rect width='10' height='10'/><text x='20' y='15'>Hi</text>", Viewport)));

        Assert.NotNull(model);
        Assert.IsAssignableFrom<Bitmap>(model.Image);
        Assert.Equal(ImageType.Svg, model.ImageType);
        Assert.Equal(40u, model.PixelWidth);
        Assert.Equal(20u, model.PixelHeight);
        Assert.Null(model.FileInfo);
    }

    [AvaloniaFact]
    public async Task LoadAsync_Png_ReturnsBitmapModelWithoutFileInfo()
    {
        var path = Path.Combine(_directory, "c.png");
        using (var image = new MagickImage(MagickColors.Red, 4, 3))
        {
            await image.WriteAsync(path, MagickFormat.Png, TestContext.Current.CancellationToken);
        }

        using var model = await TempFileSingleImage.LoadAsync(new FileInfo(path));

        Assert.NotNull(model);
        Assert.IsAssignableFrom<Bitmap>(model.Image);
        Assert.Equal(ImageType.Bitmap, model.ImageType);
        Assert.Equal(4u, model.PixelWidth);
        Assert.Equal(3u, model.PixelHeight);
        Assert.Null(model.FileInfo);
    }

    [AvaloniaFact]
    public async Task LoadAsync_SvgDeletedAfterLoading_KeepsItsImage()
    {
        var file = Write("gone-later.svg",
            Document("<rect x='5' y='5' width='10' height='10' fill='red'/>", Viewport));

        using var model = await TempFileSingleImage.LoadAsync(file);
        file.Delete();

        Assert.NotNull(model);
        Assert.IsType<SvgDrawing>(model.Image);
    }

    [AvaloniaFact]
    public async Task LoadAsync_MissingFile_ReturnsNull()
    {
        Assert.Null(await TempFileSingleImage.LoadAsync(new FileInfo(Path.Combine(_directory, "gone.png"))));
    }

    [AvaloniaFact]
    public async Task LoadAsync_MissingSvg_ReturnsNull()
    {
        Assert.Null(await TempFileSingleImage.LoadAsync(new FileInfo(Path.Combine(_directory, "gone.svg"))));
    }

    [AvaloniaFact]
    public async Task LoadAsync_GarbageSvg_ReturnsNull()
    {
        Assert.Null(await TempFileSingleImage.LoadAsync(Write("d.svg", "<svg")));
    }

    [AvaloniaFact]
    public async Task LoadAsync_FileThatIsNoImage_ReturnsNull()
    {
        Assert.Null(await TempFileSingleImage.LoadAsync(Write("e.png", "not an image")));
    }

    [AvaloniaFact]
    public async Task LoadSvgAsync_SupportedSvg_ReturnsDrawing()
    {
        var image = await TempFileSingleImage.LoadSvgAsync(Write("f.svg",
            Document("<rect x='5' y='5' width='10' height='10' fill='red'/>", Viewport)));

        Assert.IsType<SvgDrawing>(image);
    }

    private FileInfo Write(string name, string text)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, text);
        return new FileInfo(path);
    }
}
