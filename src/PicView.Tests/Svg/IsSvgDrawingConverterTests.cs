using System.Globalization;
using Avalonia.Media.Imaging;
using PicView.Avalonia.Converters;
using static PicView.Tests.Svg.SvgParserTestHelper;

namespace PicView.Tests.Svg;

public class IsSvgDrawingConverterTests
{
    private static object? Convert(object? value) =>
        IsSvgDrawingConverter.Instance.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

    [Fact]
    public void Convert_SvgDrawing_ReturnsTrue()
    {
        Assert.Equal(true, Convert(Drawing(Document("<rect width='1' height='1'/>"))));
    }

    [AvaloniaFact]
    public void Convert_Bitmap_ReturnsFalse()
    {
        using var bitmap = new WriteableBitmap(new global::Avalonia.PixelSize(2, 2), new global::Avalonia.Vector(96, 96),
            global::Avalonia.Platform.PixelFormat.Bgra8888, global::Avalonia.Platform.AlphaFormat.Premul);
        Assert.Equal(false, Convert(bitmap));
    }

    [Fact]
    public void Convert_Null_ReturnsFalse()
    {
        Assert.Equal(false, Convert(null));
    }
}
