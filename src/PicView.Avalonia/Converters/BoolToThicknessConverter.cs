using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public class BoolToThicknessConverter : IValueConverter
{
    public Thickness TrueThickness { get; set; } = new(0);
    public Thickness FalseThickness { get; set; } = new(0);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? TrueThickness : FalseThickness;

        return BindingOperations.DoNothing;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}
