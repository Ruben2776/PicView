using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PicView.Avalonia.Converters;

public enum ComparisonOperator
{
    LessThan,
    LessThanOrEqual,
    Equal,
    GreaterThanOrEqual,
    GreaterThan,
    NotEqual
}

public class ComparisonConverter : IValueConverter
{
    public double CompareTo { get; set; }
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.Equal;

    public object ProvideValue(IServiceProvider serviceProvider) => this;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return false;

        try
        {
            var numericValue = System.Convert.ToDouble(value, culture);
            var compareValue = CompareTo;

            return Operator switch
            {
                ComparisonOperator.LessThan => numericValue < compareValue,
                ComparisonOperator.LessThanOrEqual => numericValue <= compareValue,
                ComparisonOperator.Equal => Math.Abs(numericValue - compareValue) < double.Epsilon,
                ComparisonOperator.GreaterThanOrEqual => numericValue >= compareValue,
                ComparisonOperator.GreaterThan => numericValue > compareValue,
                ComparisonOperator.NotEqual => Math.Abs(numericValue - compareValue) > double.Epsilon,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}



/// <summary>
/// Returns a double "limited" by a comparison threshold.
/// Threshold is taken from ConverterParameter if provided, otherwise CompareTo.
///
/// - GreaterThan / GreaterThanOrEqual => enforce MIN:  result = (value >= t) ? value : t
/// - LessThan / LessThanOrEqual       => enforce MAX:  result = (value <= t) ? value : t
/// - Equal                           => result = t
/// - NotEqual                        => result = (value != t) ? value : t
/// </summary>
public sealed class LimitConverter : IValueConverter
{
    public double CompareTo { get; set; }
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.GreaterThanOrEqual;

    public object ProvideValue(IServiceProvider serviceProvider) => this;
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryToDouble(value, culture, out var v))
            return GetThresholdOrZero(parameter, culture);

        var t = GetThreshold(parameter, culture);

        return Operator switch
        {
            ComparisonOperator.GreaterThan =>
                v > t ? v : t,

            ComparisonOperator.GreaterThanOrEqual =>
                v >= t ? v : t,

            ComparisonOperator.LessThan =>
                v < t ? v : t,

            ComparisonOperator.LessThanOrEqual =>
                v <= t ? v : t,

            ComparisonOperator.Equal =>
                t,

            ComparisonOperator.NotEqual =>
                Math.Abs(v - t) > double.Epsilon ? v : t,

            _ => v
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private double GetThreshold(object? parameter, CultureInfo culture)
    {
        if (parameter is not null && TryToDouble(parameter, culture, out var p))
            return p;

        return CompareTo;
    }

    private static double GetThresholdOrZero(object? parameter, CultureInfo culture)
        => parameter is not null && TryToDouble(parameter, culture, out var p) ? p : 0d;

    private static bool TryToDouble(object? input, CultureInfo culture, out double result)
    {
        try
        {
            switch (input)
            {
                case null:
                    result = 0;
                    return false;
                case double d:
                    result = d;
                    return true;
                case float f:
                    result = f;
                    return true;
                case int i:
                    result = i;
                    return true;
                case long l:
                    result = l;
                    return true;
                case decimal m:
                    result = (double)m;
                    return true;
                case string s:
                    return double.TryParse(s, NumberStyles.Float, culture, out result);
                default:
                    result = System.Convert.ToDouble(input, culture);
                    return true;
            }
        }
        catch
        {
            result = 0;
            return false;
        }
    }
}
