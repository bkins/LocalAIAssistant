using System.Text;
using CP.Client.Core.Avails;
using LocalAIAssistant.Knowledge.Tasks.ViewModels;

namespace LocalAIAssistant.MarkdownFormatter;

public sealed class TaskMarkdownFormatter : MarkdownFormatterBase<TaskDetailViewModel>
{
    protected override void AppendHeader(StringBuilder sb, IReadOnlyList<TaskDetailViewModel> tasks)
    {
        sb.AppendLine($"# Tasks ({tasks.Count})");
    }

    protected override void AppendItem(StringBuilder sb, TaskDetailViewModel task)
    {
        var statusIcon = task.IsCompleted ? "✅" : "🟢";

        sb.AppendLine($"## {task.Position}. {task.ShortDescription} {statusIcon}");
        sb.AppendLine();

        if (task.DueDate < DateTimeOffset.UtcNow 
         && task.IsCompleted.Not())
        {
            sb.AppendLine("> ⚠️ **Overdue**");
            sb.AppendLine();
        }

        sb.AppendLine($"- **Status:** {(task.IsCompleted ? "Completed" : "Open")}");
        sb.AppendLine($"- **Priority:** {task.Priority}");
        sb.AppendLine($"- **Due:** {FormatDate(task.DueDate)}");
        sb.AppendLine($"- **Tags:** {task.Tags}"); //{FormatTags(task.Tags)}");
        sb.AppendLine($"- **Id:** `{task.Id}`");

        sb.AppendLine();
    }

    private static string FormatDate(DateTimeOffset? date) =>
            date.HasValue ? date.Value.ToString("yyyy-MM-dd") : "_None_";

    // private static string FormatTags(IReadOnlyList<string> tags) =>
    //         tags.Count > 0 ? string.Join(", ", tags) : "_None_";

}
