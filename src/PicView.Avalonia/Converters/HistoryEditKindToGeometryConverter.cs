using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PicView.Avalonia.History;

namespace PicView.Avalonia.Converters;

public sealed class HistoryEditKindToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not EditKind k) return null;

        var key = k switch
        {
            EditKind.Open   => "AltFolderGeometry",
            EditKind.Crop   => "CropGeometry",
            EditKind.Rotate => "RotateGeometry",
            EditKind.FlipH  => "FlipHorizontalGeometry",
            EditKind.FlipV  => "FlipVerticalGeometry",
            EditKind.Effect => "FlaskGeometry",
            EditKind.Resize => "M4,20 L12,12 M8,20 H4 V16 M20,4 L12,12 M16,4 H20 V8",
            _               => "M11,11 L13,11 L13,13 L11,13 Z"
        };

        var app = Application.Current;
        if (app != null &&
            app.TryFindResource(key, app.ActualThemeVariant, out var res) &&
            res is Geometry g)
        {
            return g;
        }

        return Geometry.Parse("M11,11 L13,11 L13,13 L11,13 Z");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}