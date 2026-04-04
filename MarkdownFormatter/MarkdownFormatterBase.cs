using System.Text;

namespace LocalAIAssistant.MarkdownFormatter;

public abstract class MarkdownFormatterBase<T> : IMarkdownFormatter<T>
{
    public string Format(IReadOnlyList<T> items)
    {
        if (items.Count == 0)
            return "_No results found._";

        var sb = new StringBuilder();

        AppendHeader(sb, items);
        sb.AppendLine();

        foreach (var item in items)
        {
            AppendItem(sb, item);
        }

        return sb.ToString();
    }

    protected virtual void AppendHeader(StringBuilder sb, IReadOnlyList<T> items)
    {
        sb.AppendLine($"# Results ({items.Count})");
    }

    protected abstract void AppendItem(StringBuilder sb, T item);
}
