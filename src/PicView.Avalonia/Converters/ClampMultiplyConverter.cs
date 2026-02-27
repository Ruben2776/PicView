using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public sealed class ClampMultiplyConverter : IValueConverter
{
    public double Min { get; set; } = 120;
    public double Max { get; set; } = 420;

    public object ProvideValue(IServiceProvider serviceProvider) => this;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value switch
        {
            double d => d,
            float f => f,
            _ => 0d
        };

        var factor = 0.22;
        if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            factor = parsed;

        var scaled = v * factor;
        if (scaled < Min) scaled = Min;
        if (scaled > Max) scaled = Max;

        return scaled;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
