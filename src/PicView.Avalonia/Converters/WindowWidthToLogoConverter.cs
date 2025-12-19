using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public sealed class WindowWidthToLogoConverter : IValueConverter
{
    public double Breakpoint { get; set; } = 900;
    public string SmallKey { get; set; } = "LogoImage";
    public string LargeKey { get; set; } = "LogoFullImage";

    public object ProvideValue(IServiceProvider serviceProvider) => this;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var width = value switch
        {
            double d => d,
            float f => f,
            _ => 0d
        };

        var key = width > Breakpoint ? LargeKey : SmallKey;

        return Application.Current?.TryFindResource(key, out var res) == true
            ? res
            : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
