using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public class IndexBiggerThanBooleanConverter: IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!int.TryParse(value?.ToString(), out var index) || !int.TryParse(parameter?.ToString(), out var parameterIndex))
        {
            return BindingOperations.DoNothing;
        }
        return index >= parameterIndex;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        
        return BindingOperations.DoNothing;
    }
}
