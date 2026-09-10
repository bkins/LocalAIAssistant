using System.Globalization;
using LocalAIAssistant.Knowledge.Inbox;
using Microsoft.Maui.Controls;

namespace LocalAIAssistant.Presentation;

/// <summary>
/// XAML bridge for the read-only Knowledge Inbox presentation pilot.
/// </summary>
public sealed class KnowledgeItemPresentationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is KnowledgeItem item ? KnowledgeItemPresentationAdapter.Create(item) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Knowledge Inbox presentation is read-only.");
}
