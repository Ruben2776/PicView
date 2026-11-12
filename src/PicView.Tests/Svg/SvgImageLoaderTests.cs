using System.Globalization;
using System.IO.Compression;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Avalonia.Converters;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Svg;
using PicView.Core.ImageDecoding;
using PicView.Core.Svg;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

[Collection("Sequential")]
public class SvgImageLoaderTests : IDisposable
{
    private const string Viewport = "width='40' height='20'";

    private static readonly string ConvertibleSvg = Document("<rect x='5' y='5' width='10' height='10' fill='red'/>", Viewport);

    private static readonly string UnsupportedSvg =
        Document("<rect x='5' y='5' width='10' height='10' fill='red'/><text x='20' y='15'>Hi</text>", Viewport);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"PicView.SvgImageLoaderTests.{Guid.NewGuid():N}");

    public SvgImageLoaderTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task LoadAsync_Convertible_ReturnsDrawing()
    {
        var result = await SvgImageLoader.LoadAsync(Write("convertible.svg", ConvertibleSvg), 40, 20);

        var drawing = Assert.IsType<SvgDrawing>(result);
        Assert.Equal(40, drawing.Width);
        Assert.Equal(20, drawing.Height);
    }

    [Fact]
    public void Themed_SetsCurrentColorAndInitialFillToTheTextColor()
    {
        var textColor = new SvgColor(255, 220, 221, 222);

        var options = SvgImageLoader.Themed(textColor);

        Assert.Equal(textColor, options.CurrentColor);
        Assert.Equal(textColor, options.InitialFill);
    }

    [Fact]
    public void Themed_IconMixingCurrentColorAndUnsetFill_DrawsBothInTheTextColor()
    {
        // help-sharp.svg from Ionicons: a currentColor stroke beside a rect with no fill set
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 512 512'>" +
                           "<path d='M160 164c0-10 1.44-33 33.54-59.46' fill='none' stroke='currentColor' stroke-width='40'/>" +
                           "<rect x='220' y='368' width='56' height='56' rx='3.5' ry='3.5'/></svg>";
        var textColor = new SvgColor(255, 220, 221, 222);

        var drawing = SvgParser.Parse(svg, SvgImageLoader.Themed(textColor)).Drawing;

        Assert.NotNull(drawing);
        var shapes = drawing.Root.Children.OfType<SvgShape>().ToList();
        Assert.Equal(2, shapes.Count);
        Assert.Equal(new SvgSolidPaint(textColor), shapes[0].Stroke?.Paint);
        Assert.Equal(new SvgSolidPaint(textColor), shapes[1].Fill);
    }

    [Theory]
    [InlineData("<svg><path stroke='currentColor'/></svg>", true)]
    [InlineData("<svg><path stroke='CURRENTCOLOR'/></svg>", true)]
    [InlineData("<svg><rect width='1' height='1'/></svg>", false)]
    public void UsesCurrentColor_FindsTheKeyword(string svg, bool expected) =>
        Assert.Equal(expected, SvgImageLoader.UsesCurrentColor(svg));

    [AvaloniaFact]
    public async Task LoadAsync_Unsupported_ReturnsBitmapAtSvgSize()
    {
        var result = await SvgImageLoader.LoadAsync(Write("unsupported.svg", UnsupportedSvg), 40, 20);

        using var bitmap = Assert.IsAssignableFrom<Bitmap>(result);
        Assert.Equal(40, bitmap.Size.Width, 1);
        Assert.Equal(20, bitmap.Size.Height, 1);
    }

    [AvaloniaFact]
    public async Task LoadAsync_Garbage_ReturnsNull()
    {
        var result = await SvgImageLoader.LoadAsync(Write("garbage.svg", "not an svg"), 10, 10);

        Assert.Null(result);
    }

    [AvaloniaFact]
    public async Task GetImageModelAsync_Convertible()
    {
        using var model = await GetImageModel.GetImageModelAsync(Write("convertible.svg", ConvertibleSvg));

        Assert.IsType<SvgDrawing>(model.Image);
        Assert.Equal(ImageType.Svg, model.ImageType);
        Assert.Equal(40u, model.PixelWidth);
        Assert.Equal(20u, model.PixelHeight);
    }

    [AvaloniaFact]
    public async Task GetImageModelAsync_Unsupported()
    {
        using var model = await GetImageModel.GetImageModelAsync(Write("unsupported.svg", UnsupportedSvg));

        Assert.IsAssignableFrom<Bitmap>(model.Image);
        Assert.Equal(ImageType.Svg, model.ImageType);
        Assert.Equal(40u, model.PixelWidth);
        Assert.Equal(20u, model.PixelHeight);
    }

    [AvaloniaFact]
    public async Task Displayable_ConvertedAndFallback_HaveSameSize()
    {
        using var converted = await GetImageModel.GetImageModelAsync(Write("convertible.svg", ConvertibleSvg));
        using var fallback = await GetImageModel.GetImageModelAsync(Write("unsupported.svg", UnsupportedSvg));
        var converter = new DisplayableConverter();

        var convertedImage = Assert.IsAssignableFrom<IImage>(
            converter.Convert(converted.Image, typeof(IImage), null, CultureInfo.InvariantCulture));
        var fallbackImage = Assert.IsAssignableFrom<IImage>(
            converter.Convert(fallback.Image, typeof(IImage), null, CultureInfo.InvariantCulture));

        Assert.Equal(convertedImage.Size.Width, fallbackImage.Size.Width, 1);
        Assert.Equal(convertedImage.Size.Height, fallbackImage.Size.Height, 1);
    }

    [AvaloniaFact]
    public async Task GetImageModelAsync_Svgz()
    {
        var path = Path.Combine(_directory, "convertible.svgz");
        await using (var file = File.Create(path))
        await using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
        {
            await gzip.WriteAsync(Encoding.UTF8.GetBytes(ConvertibleSvg));
        }

        using var model = await GetImageModel.GetImageModelAsync(new FileInfo(path));

        Assert.IsType<SvgDrawing>(model.Image);
    }

    private FileInfo Write(string name, string content)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }
}
