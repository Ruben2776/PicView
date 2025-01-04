using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using PicView.Core.Localization;

namespace PicView.Avalonia.Converters;

public class IndexToStringSizeConverter: IValueConverter
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
                return TranslationHelper.Translation.Thumbnail ?? "Thumb";
            
            case 2 when parameterIndex is 1:
                return "medium";
            case 2:
                return "small";
            
            case 3 when parameterIndex is 1:
                return "large";
            case 3 when parameterIndex is 2:
                return "medium";
            case 3:
                return "small";
            
            case 4 when parameterIndex is 1:
                return "large";
            case 4 when parameterIndex is 2:
                return "medium";
            case 4 when parameterIndex is 3:
                return "small";
            case 4:
                return "xs";
            
            case 5 when parameterIndex is 1:
                return "xl";
            case 5 when parameterIndex is 2:
                return "large";
            case 5 when parameterIndex is 3:
                return "medium";
            case 5when parameterIndex is 4:
                return "small";
            case 5:
                return "xs";
            
            
            case 6 when parameterIndex is 1:
                return "xl";
            case 6 when parameterIndex is 2:
                return "large";
            case 6 when parameterIndex is 3:
                return "medium";
            case 6 when parameterIndex is 4:
                return "small";
            case 6 when parameterIndex is 5:
                return "xs";
            case 6:
                return "xxs";
            
            case 7 when parameterIndex is 1:
                return "xxl";
            case 7 when parameterIndex is 2:
                return "xl";
            case 7 when parameterIndex is 3:
                return "large";
            case 7 when parameterIndex is 4:
                return "medium";
            case 7 when parameterIndex is 5:
                return "small";
            case 7 when parameterIndex is 6:
                return "xs";
            case 7:
                return "xxs";
            default:
                return BindingOperations.DoNothing;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
