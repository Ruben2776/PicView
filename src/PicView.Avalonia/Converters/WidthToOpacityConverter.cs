using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public enum WidthToOpacityMode
{
    Narrow,
    Wide
}

public sealed class WidthToOpacityConverter : IValueConverter
{
    public double Breakpoint { get; set; } = 800;
    public WidthToOpacityMode WidthMode { get; set; } = WidthToOpacityMode.Narrow;

    

    public object ProvideValue(IServiceProvider serviceProvider) => this;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var width = value switch
        {
            double d => d,
            float f => f,
            _ => 0d
        };

        var isWide = width > Breakpoint;

        return WidthMode switch
        {
            WidthToOpacityMode.Wide => isWide ? 1d : 0d,
            WidthToOpacityMode.Narrow => isWide ? 0d : 1d,
            _ => isWide ? 1d : 0d
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
