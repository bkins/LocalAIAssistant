namespace LocalAIAssistant.Knowledge.Inbox;

public interface ILocalKnowledgeStore
{
    void                         Save(KnowledgeItem item);
    KnowledgeItem?               Get(Guid           id);
    IReadOnlyList<KnowledgeItem> List();
    void                         Clear();
}