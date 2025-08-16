using System.Globalization;

namespace LocalAIAssistant.Converters;

public class NullToBoolConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object value
                        , Type targetType
                        , object parameter
                        , CultureInfo culture)
    {
        bool result = value != null;
        return Invert 
                ? ! result 
                : result;
    }

    public object ConvertBack(object value
                            , Type targetType
                            , object parameter
                            , CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}