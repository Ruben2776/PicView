using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

/// <summary>
/// Docks one image of a Side-by-Side pair: the first image (no parameter) goes left and the second (parameter
/// "Secondary") right, or the other way round when the order is right to left.
/// </summary>
public class SideBySideDockConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isRightToLeft = value is true;
        var isSecondary = parameter is "Secondary";
        return isRightToLeft != isSecondary ? Dock.Right : Dock.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
