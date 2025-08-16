using System.Globalization;

namespace LocalAIAssistant.Converters;

public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string level)
        {
            return level.ToUpper() switch
            {
                "ERR" or "ERROR"                      => Colors.Red
              , "WRN" or "WARN" or "WARNING"          => Colors.Orange
              , "INF" or "INFO" or "INFORMATION"      => Colors.Blue
              , "DBG" or "DEBUG" => Colors.Gray, _ => Colors.Gray
            };
        }
        
        return Colors.Gray;
    }

    public object ConvertBack(object value
                            , Type targetType
                            , object parameter
                            , CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 