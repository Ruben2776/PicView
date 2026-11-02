using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Svg.Skia;

namespace PicView.Avalonia.Converters;

/// <summary>
/// The displayable of what a tab shows: the image the viewer draws for the object the shown model holds. A decoded
/// SVG is an <see cref="SvgSource"/>, which only an <see cref="SvgImage"/> can draw, and an <see cref="SvgImage"/>
/// must be made on the UI thread, where bindings evaluate. So the wrap lives here, and Core sets the shown image
/// whatever its type.
/// </summary>
public sealed class DisplayableConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SvgSource svgSource => new SvgImage { Source = svgSource },
        IImage image => image,
        _ => null
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
