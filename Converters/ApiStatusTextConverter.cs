using System.Globalization;

namespace LocalAIAssistant.Converters;

public class ApiStatusTextConverter : IValueConverter
{

    public object Convert(object value
                        , Type targetType
                        , object parameter
                        , CultureInfo culture)
    {
        bool isAvailable = value is bool and true;
        return isAvailable ? "Ollama API: Online" : "Ollama API: Offline";
    }

    public object? ConvertBack(object? value
                             , Type targetType
                             , object? parameter
                             , CultureInfo culture) => throw new NotImplementedException();
}