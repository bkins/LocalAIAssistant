using System.Globalization;
using LocalAIAssistant.Knowledge.Inbox;

namespace LocalAIAssistant.Converters;

public class KnowledgeKindToColorConverter : IValueConverter
{
    public object Convert( object value
                         , Type targetType
                         , object parameter
                         , CultureInfo culture )
    {
        if (value is not KnowledgeKind kind)
            return Colors.Gray;

        return kind switch
        {
                KnowledgeKind.Journal => Color.FromArgb("#8B5CF6")
              , KnowledgeKind.Task    => Color.FromArgb("#4CAF50")
              , KnowledgeKind.Pending => Color.FromArgb("#FF9844")
              , _                     => Colors.Gray
        };
    }

    public object ConvertBack( object value
                             , Type targetType
                             , object parameter
                             , CultureInfo culture )
        => throw new NotImplementedException();
}
