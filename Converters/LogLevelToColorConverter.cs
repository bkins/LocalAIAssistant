using System.Globalization;

namespace LocalAIAssistant.Converters;

public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object? value
                        , Type targetType
                        , object? parameter
                        , CultureInfo culture)
    {
        if (value is string level)
        {
            return level.ToUpperInvariant() switch
            {
                "ERR" or "ERROR" or "CRITICAL" or "FATAL" => Color.FromArgb("#E53935")
              , "WRN" or "WARN" or "WARNING"             => Color.FromArgb("#FB8C00")
              , "INF" or "INFO" or "INFORMATION"         => Color.FromArgb("#1E88E5")
              , "DBG" or "DEBUG" or "TRC" or "TRACE"     => Color.FromArgb("#78909C")
              , _                                        => Color.FromArgb("#9E9E9E")
            };
        }
        
        return Color.FromArgb("#9E9E9E");
    }

    public object? ConvertBack(object? value
                              , Type targetType
                              , object? parameter
                              , CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 