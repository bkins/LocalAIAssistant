using System.Globalization;
using CP.Client.Core.Avails;

namespace LocalAIAssistant.Converters;

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert (object?     value
                         , Type        targetType
                         , object?     parameter
                         , CultureInfo culture)
    {
        if (value is bool b)
            return b.Not();

        return true; // default to visible if binding is unexpected
    }

    public object ConvertBack (object?     value
                             , Type        targetType
                             , object?     parameter
                             , CultureInfo culture)
    {
        if (value is bool b)
            return b.Not();

        return false;
    }
}