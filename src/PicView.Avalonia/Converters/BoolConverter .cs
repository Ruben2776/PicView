using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public enum BoolLogicOperator
{
    And,
    Or
}

public class BoolLogicConverter : IMultiValueConverter
{
    public BoolLogicOperator Operator { get; set; } = BoolLogicOperator.And;

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Count == 0)
            return false;

        return Operator switch
        {
            BoolLogicOperator.And => EvaluateAnd(values),
            BoolLogicOperator.Or  => EvaluateOr(values),
            _                     => false
        };
    }

    private static bool EvaluateAnd(IList<object?> values)
    {
        foreach (var v in values)
        {
            if (v is bool b)
            {
                if (!b)
                    return false;
            }
            else
            {
                // treat null / non-bool as false
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateOr(IList<object?> values)
    {
        foreach (var v in values)
        {
            if (v is bool b && b)
                return true;
        }

        return false;
    }

    public object? ConvertBack(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
