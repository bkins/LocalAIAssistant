using System.Globalization;

namespace LocalAIAssistant.Converters;

public sealed class TagsToTextConverter : IValueConverter
{
    public object Convert (object?     value
                         , Type        targetType
                         , object?     parameter
                         , CultureInfo culture)
    {
        if (value is not IEnumerable<string> tags)
            return string.Empty;

        var list = tags.Select(tag => tag?.Trim())
                       .Where(tag => !string.IsNullOrWhiteSpace(tag))
                       .ToList();

        if (list.Count == 0)
            return string.Empty;

        // display style: #work  #j03
        return string.Join("  "
                         , list.Select(t => $"#{t}"));
    }

    public object ConvertBack (object?     value
                             , Type        targetType
                             , object?     parameter
                             , CultureInfo culture)
        => throw new NotSupportedException();
}