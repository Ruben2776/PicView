using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PicView.Avalonia.Svg;
using PicView.Core.Svg;

namespace PicView.Avalonia.Converters;

/// <summary>
/// The displayable of what a tab shows: the image the viewer draws for the object the shown model holds. A parsed
/// SVG is an <see cref="SvgDrawing"/>, which becomes a <see cref="DrawingImage"/> here, and a
/// <see cref="DrawingImage"/> must be made on the UI thread, where bindings evaluate. So the build lives here, and
/// Core sets the shown image whatever its type.
/// </summary>
public sealed class DisplayableConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SvgDrawing drawing => DrawingImageBuilder.Build(drawing),
        IImage image => image,
        _ => null
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
