using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public class IntToEnumBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int intVal || parameter == null)
        {
            return false;
        }

        try
        {
            return intVal == System.Convert.ToInt32(parameter, CultureInfo.InvariantCulture);
        }
        catch
        {
            return false;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter != null)
        {
            return System.Convert.ToInt32(parameter, CultureInfo.InvariantCulture);
        }
        return BindingOperations.DoNothing;
    }
}
