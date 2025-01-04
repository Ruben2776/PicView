using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public class IndexToPercentageSizeConverter: IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!int.TryParse(value?.ToString(), out var index) || !int.TryParse(parameter?.ToString(), out var parameterIndex))
        {
            return BindingOperations.DoNothing;
        }

        switch (index)
        {
            case 1:
                return 30;
            
            case 2 when parameterIndex is 1:
                return 30;
            case 2:
                return 50;
            
            case 3 when parameterIndex is 1:
                return 70;
            case 3 when parameterIndex is 2:
                return 50;
            case 3:
                return 30;
            
            case 4 when parameterIndex is 1:
                return 70;
            case 4 when parameterIndex is 2:
                return 50;
            case 4 when parameterIndex is 3:
                return 30;
            case 4:
                return 15;
            
            case 5 when parameterIndex is 1:
                return 80;
            case 5 when parameterIndex is 2:
                return 70;
            case 5 when parameterIndex is 3:
                return 50;
            case 5when parameterIndex is 4:
                return 30;
            case 5:
                return 15;
            
            
            case 6 when parameterIndex is 1:
                return 80;
            case 6 when parameterIndex is 2:
                return 70;
            case 6 when parameterIndex is 3:
                return 60;
            case 6 when parameterIndex is 4:
                return 50;
            case 6 when parameterIndex is 5:
                return 40;
            case 6:
                return 30;
            
            case 7 when parameterIndex is 1:
                return 85;
            case 7 when parameterIndex is 2:
                return 75;
            case 7 when parameterIndex is 3:
                return 65;
            case 7 when parameterIndex is 4:
                return 50;
            case 7 when parameterIndex is 5:
                return 40;
            case 7 when parameterIndex is 6:
                return 30;
            case 7:
                return 20;
            default:
                return BindingOperations.DoNothing;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
