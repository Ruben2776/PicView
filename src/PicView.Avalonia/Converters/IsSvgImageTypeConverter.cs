using System.Globalization;
using Avalonia.Data.Converters;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.Converters;

/// <summary>
/// True when what a tab shows is an SVG, whether drawn as a parsed SVG or as a fallback bitmap. It is also true for
/// an SVG that is not a file, such as pasted SVG code, which <see cref="IsSvgFileConverter"/> cannot see.
/// </summary>
public sealed class IsSvgImageTypeConverter : IValueConverter
{
    public static readonly IsSvgImageTypeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ImageType.Svg;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
