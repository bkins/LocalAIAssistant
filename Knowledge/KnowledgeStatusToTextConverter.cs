using System.Globalization;
using LocalAIAssistant.Knowledge.Inbox;

namespace LocalAIAssistant.Knowledge;

public sealed class KnowledgeStatusToTextConverter : IValueConverter
{
    public object Convert(object? value
                        , Type targetType
                        , object? parameter
                        , CultureInfo culture)
        => value is KnowledgeStatus status 
                   ? status.ToString() 
                   : string.Empty;

    object? IValueConverter.ConvertBack (object?     value
                                       , Type        targetType
                                       , object?     parameter
                                       , CultureInfo culture) => ConvertBack(value
                                                                           , targetType
                                                                           , parameter
                                                                           , culture);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}