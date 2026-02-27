using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace PicView.Avalonia.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public IBrush ActiveBrush { get; set; } = new SolidColorBrush(Color.FromRgb(70, 130, 180)); // steel blue
    public IBrush DefaultBrush { get; set; } = new SolidColorBrush(Color.FromRgb(34, 34, 34));  // dark gray

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? ActiveBrush : DefaultBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class BoolToOpacityConverter : IValueConverter
{
    public double ActiveOpacity { get; set; } = 1.0;
    public double InactiveOpacity { get; set; } = 0.4;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? InactiveOpacity : ActiveOpacity;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
