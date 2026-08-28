using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace LocalAIAssistant.Converters;

/// <summary>
/// Converts a boolean to one of two integer values specified via ConverterParameter.
/// Parameter format: "trueValue|falseValue" (e.g., "2|1" returns 2 when true, 1 when false).
/// </summary>
public class BoolToIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isBool = value is bool boolVal && boolVal;

        if (parameter is not string paramStr || !paramStr.Contains('|'))
        {
            return isBool ? 1 : 0;
        }

        var parts = paramStr.Split('|');
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var trueVal)
            || !int.TryParse(parts[1], out var falseVal))
        {
            return isBool ? 1 : 0;
        }

        return isBool ? trueVal : falseVal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
