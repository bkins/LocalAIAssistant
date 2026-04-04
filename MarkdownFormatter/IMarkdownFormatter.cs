namespace LocalAIAssistant.MarkdownFormatter;

public interface IMarkdownFormatter<T>
{
    string Format(IReadOnlyList<T> items);
}
