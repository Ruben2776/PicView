using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public sealed class HistoryRedoGreyOpacityConverter : IMultiValueConverter
{
    // values[0] = isRedoBranch (bool)
    // values[1] = isSelected (bool)
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isRedo = values.Count > 0 && values[0] is bool b0 && b0;
        var isSel  = values.Count > 1 && values[1] is bool b1 && b1;

        // Grey only when redo-branch AND not selected
        return (isRedo && !isSel) ? 0.55 : 1.0;
    }
}
