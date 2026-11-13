using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Avalonia.Converters;
using PicView.Core.Svg;

namespace PicView.Tests.Controls;

/// <summary>
/// The displayable of a shown model: the one place an SvgDrawing becomes the image the viewer draws.
/// </summary>
[Collection("Sequential")]
public class DisplayableConverterTests
{
    private readonly DisplayableConverter _converter = new();

    [AvaloniaFact]
    public void Convert_SvgDrawing_BuildsDrawingImage()
    {
        var drawing = new SvgDrawing(1, 1, new SvgGroup(null, 1, null, []));

        var result = _converter.Convert(drawing, typeof(IImage), null, CultureInfo.InvariantCulture);

        var image = Assert.IsType<DrawingImage>(result);
        Assert.Equal(new Size(1, 1), image.Size);
    }

    [AvaloniaFact]
    public void Convert_SameDrawingTwice_ReturnsSameImage()
    {
        // Every binding that shows the drawing (viewer, zoom previewer, tab tooltip) has its own converter
        var drawing = new SvgDrawing(1, 1, new SvgGroup(null, 1, null, []));

        var first = new DisplayableConverter().Convert(drawing, typeof(IImage), null, CultureInfo.InvariantCulture);
        var second = new DisplayableConverter().Convert(drawing, typeof(IImage), null, CultureInfo.InvariantCulture);

        Assert.IsType<DrawingImage>(first);
        Assert.Same(first, second);
    }

    [AvaloniaFact]
    public void Convert_DifferentDrawings_ReturnsDifferentImages()
    {
        // SvgDrawing is a record: equal values must still be different images, since they are different files
        var root = new SvgGroup(null, 1, null, []);
        var first = _converter.Convert(new SvgDrawing(1, 1, root), typeof(IImage), null, CultureInfo.InvariantCulture);
        var second = _converter.Convert(new SvgDrawing(1, 1, root), typeof(IImage), null, CultureInfo.InvariantCulture);

        Assert.NotSame(first, second);
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
