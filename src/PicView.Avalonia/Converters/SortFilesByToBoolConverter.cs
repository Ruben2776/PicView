using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.Converters;

public class SortFilesByToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var sortOrder = FileListHelper.GetSortOrder();
        if (Enum.TryParse<FileListHelper.SortFilesBy>(parameter as string, true, out var result))
        {
            return sortOrder == result;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is Enum enumValue)
        {
            return enumValue;
        }
        return BindingOperations.DoNothing;
    }
}