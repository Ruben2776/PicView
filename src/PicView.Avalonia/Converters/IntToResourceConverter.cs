using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public class IntResourceMapping
{
    public int Key { get; set; }
    public string? ResourceKey { get; set; }
}

public class IntToResourceConverter : IValueConverter
{
    public IList<IntResourceMapping> Mappings { get; } = new List<IntResourceMapping>();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int intValue)
            return null;

        var mapping = Mappings.FirstOrDefault(m => m.Key == intValue);
        if (mapping?.ResourceKey is null)
            return null;

        if (Application.Current is { } app && app.TryFindResource(mapping.ResourceKey, out var resource))
            return resource;

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
