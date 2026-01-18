using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.Knowledge.Journals.ViewModels;

public sealed class JournalRevisionHistoryViewModel
{
    public IReadOnlyList<JournalRevisionDto> Revisions { get; }

    public bool HasRevisions => Revisions.Count > 0;
}
