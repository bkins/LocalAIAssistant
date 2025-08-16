using System.Globalization;

namespace LocalAIAssistant.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object      value
                        , Type        targetType
                        , object      parameter
                        , CultureInfo culture)
    {
        var isAvailable = value is bool and true;
        return isAvailable 
                ? Colors.LimeGreen 
                : Colors.Red;
    }

    public object? ConvertBack(object?     value
                             , Type        targetType
                             , object?     parameter
                             , CultureInfo culture) => throw new NotImplementedException();

}