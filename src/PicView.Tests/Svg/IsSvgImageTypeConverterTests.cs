using System.Globalization;
using PicView.Avalonia.Converters;
using PicView.Core.ImageDecoding;

namespace PicView.Tests.Svg;

public class IsSvgImageTypeConverterTests
{
    private static object? Convert(object? value) =>
        IsSvgImageTypeConverter.Instance.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(ImageType.Svg, true)]
    [InlineData(ImageType.Bitmap, false)]
    [InlineData(ImageType.Invalid, false)]
    public void Convert_ImageType_IsTrueOnlyForSvg(ImageType type, bool expected)
    {
        Assert.Equal(expected, Convert(type));
    }

    [Fact]
    public void Convert_Null_ReturnsFalse()
    {
        Assert.Equal(false, Convert(null));
    }
}
