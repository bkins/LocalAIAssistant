using System.Globalization;
using LocalAIAssistant.Core.Time;

namespace LocalAIAssistant.Converters;

public class UtcToLocalDateTimeConverter : IValueConverter
{
    public object? Convert(object?     value
                         , Type        targetType
                         , object?     parameter
                         , CultureInfo culture)
    {
        // Defensive: non-DateTime / null input returns unchanged so the whole
        // page still renders if a binding misbehaves.
        if (value is DateTime dateTime)
            return TimestampConversion.ToLocalSafe(dateTime);

        return value;
    }

    public object? ConvertBack(object?     value
                             , Type        targetType
                             , object?     parameter
                             , CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
