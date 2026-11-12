using System.Globalization;
using Avalonia.Data.Converters;
using PicView.Core.Svg;

namespace PicView.Avalonia.Converters;

/// <summary>
/// True when the shown image of a tab is a parsed SVG, the only image Copy AXAML can convert.
/// </summary>
public sealed class IsSvgDrawingConverter : IValueConverter
{
    public static readonly IsSvgDrawingConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is SvgDrawing;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
