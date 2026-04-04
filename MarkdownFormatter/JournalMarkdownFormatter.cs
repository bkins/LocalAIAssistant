using System.Text;
using LocalAIAssistant.Knowledge.Journals.ViewModels;

namespace LocalAIAssistant.MarkdownFormatter;

public sealed class JournalMarkdownFormatter : MarkdownFormatterBase<JournalDetailViewModel>
{
    protected override void AppendHeader(StringBuilder sb, IReadOnlyList<JournalDetailViewModel> entries)
    {
        sb.AppendLine($"# Journal Entries ({entries.Count})");
    }

    protected override void AppendItem(StringBuilder sb, JournalDetailViewModel entry)
    {
        sb.AppendLine($"## {entry.CreatedAt:yyyy-MM-dd}");
        sb.AppendLine();

        sb.AppendLine(entry.Text);
        sb.AppendLine();

        if (entry.Tags.Count > 0)
        {
            sb.AppendLine($"- **Tags:** {string.Join(", ", entry.Tags)}");
        }
        
        sb.AppendLine($"- **Mood:** {entry.Mood} - {ToEmoji(entry.MoodScore ?? 0)} [{ToBar(entry.MoodScore ?? 0)}]");

        sb.AppendLine($"- **Id:** `{entry.JournalId}`");
        sb.AppendLine();
    }
    
    public static string ToEmoji(int mood)
    {
        var emoji = mood switch
        {
                1 => "😞" // VeryNegative
              , 2 => "🙁" // Negative
              , 3 => "😐" // Neutral
              , 4 => "🙂" // Positive
              , 5 => "😄" // VeryPositive
              , _ => "❓"
        };

        var bar = ToBar(mood);

        return $"{emoji} {mood} {bar} ({mood}/5)";
    }

    private static string ToBar(int value)
    {
        const int max = 5;

        return new string('█', value)
             + new string('░', max - value);
    }

    public string ToEnglish(int mood)
    {
        return mood switch
        {
                1 => "Very Negative"
              , 2 => "Negative"
              , 3 => "Neutral"
              , 4 => "Positive"
              , 5 => "Very Positive"
              , _ => mood.ToString()
        };
    }
}
