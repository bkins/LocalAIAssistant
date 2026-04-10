using System.Globalization;
using LocalAIAssistant.Core.Parsing;

namespace LocalAIAssistant.CognitivePlatform.Rendering.Converters;

public class PriorityIconConverter : IValueConverter
{
    public object Convert(object value
                        , Type targetType
                        , object parameter
                        , CultureInfo culture)
    {
        var task = value as ParsedTask;
        if (task == null) return "";

        if (task.IsImportant && task.IsUrgent) return "🟥"; // Do now
        if (task.IsImportant)                 return "🟧";  // Important
        if (task.IsUrgent)                    return "🟨";  // Urgent
        return "⬜";                                         // Normal
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}