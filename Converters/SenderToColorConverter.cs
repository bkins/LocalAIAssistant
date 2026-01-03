using System.Globalization;
using LocalAIAssistant.Data;

namespace LocalAIAssistant.Converters;

public class SenderToColorConverter: IValueConverter
{
    public object Convert(object value
                        , Type targetType
                        , object parameter
                        , CultureInfo culture)
    {
        return (value?.ToString()?.ToLower()) switch
        {
            Senders.UserLowered => Colors.LightBlue
          , Senders.AiLowered => Colors.LightGray
          , _ => Colors.White
        };
    }

    public object ConvertBack(object value
                            , Type targetType
                            , object parameter
                            , CultureInfo culture) =>
        throw new NotImplementedException();
}