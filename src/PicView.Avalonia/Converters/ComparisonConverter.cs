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