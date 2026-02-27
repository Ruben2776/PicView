using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Navigation;
using PicView.Core.Config;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.Localization;

namespace PicView.Avalonia.Converters;

public class NavigationConverter : IMultiValueConverter
{
    public NavigationDirection Direction { get; set; }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {

        var imageSource = values.Count > 0 ? values[0] : null;
        var isLooping   = values.Count > 1 && values[1] is bool b && b;

        if(imageSource is null)
            return false;

        if(isLooping)
            return true;

        if(NavigationManager.GetCurrentIndex == 0 && Direction == NavigationDirection.Previous)
            return false;

        if(NavigationManager.GetCurrentIndex == NavigationManager.GetCount - 1 && Direction == NavigationDirection.Next) 
            return false;

        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}