using System.Globalization;
using PicView.Avalonia.Converters;

namespace PicView.Tests.Svg;

public class IsSvgFileConverterTests
{
    private static object? Convert(object? value) =>
        IsSvgFileConverter.Instance.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("image.svg", true)]
    [InlineData("image.SVGZ", true)]
    [InlineData("image.png", false)]
    [InlineData("image.svg.txt", false)]
    public void Convert_FileInfo_DecidesByExtension(string fileName, bool expected)
    {
        Assert.Equal(expected, Convert(new FileInfo(fileName)));
    }

    [Fact]
    public void Convert_Null_ReturnsFalse()
    {
        Assert.Equal(false, Convert(null));
    }
}
