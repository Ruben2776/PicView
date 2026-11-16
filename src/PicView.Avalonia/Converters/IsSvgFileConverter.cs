using System.Globalization;
using Avalonia.Data.Converters;
using PicView.Core.Svg;

namespace PicView.Avalonia.Converters;

/// <summary>
/// True when a tab's file is an .svg or .svgz, whether it is shown as a parsed SVG or as a fallback bitmap. Copy SVG
/// copies the file's source text, so it works for both.
/// </summary>
public sealed class IsSvgFileConverter : IValueConverter
{
    public static readonly IsSvgFileConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FileInfo file && SvgFileReader.HasSvgExtension(file.Name);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
