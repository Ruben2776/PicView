using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

/// <summary>
/// Shows a bool as the index of a two-item ComboBox: false is 0 and true is 1. An index other than 0 or 1, as -1
/// when nothing is selected, changes nothing.
/// </summary>
public class BoolToIndexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 1 : 0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            0 => false,
            1 => true,
            _ => BindingOperations.DoNothing
        };
}
