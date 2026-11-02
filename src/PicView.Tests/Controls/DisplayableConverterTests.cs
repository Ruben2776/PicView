using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using PicView.Avalonia.Converters;

namespace PicView.Tests.Controls;

/// <summary>
/// The displayable of a shown model: the one place an SvgSource becomes the image the viewer draws.
/// </summary>
[Collection("Sequential")]
public class DisplayableConverterTests
{
    private readonly DisplayableConverter _converter = new();

    [AvaloniaFact]
    public void Convert_SvgSource_WrapsInSvgImage()
    {
        var source = SvgSource.LoadFromSvg("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1\" height=\"1\"/>");

        var result = _converter.Convert(source, typeof(IImage), null, CultureInfo.InvariantCulture);

        var image = Assert.IsType<SvgImage>(result);
        Assert.Same(source, image.Source);
    }

    [AvaloniaFact]
    public void Convert_Bitmap_PassesThrough()
    {
        using var bitmap = new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96));

        var result = _converter.Convert(bitmap, typeof(IImage), null, CultureInfo.InvariantCulture);

        Assert.Same(bitmap, result);
    }

    [AvaloniaFact]
    public void Convert_OtherOrNull_ReturnsNull()
    {
        Assert.Null(_converter.Convert("text", typeof(IImage), null, CultureInfo.InvariantCulture));
        Assert.Null(_converter.Convert(null, typeof(IImage), null, CultureInfo.InvariantCulture));
    }
}
