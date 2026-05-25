namespace LocalAIAssistant.Knowledge.Inbox;

public sealed class KnowledgeItemGroup : List<KnowledgeItem>
{
    public KnowledgeKind Kind        { get; }
    public string        DomainLabel => Kind.ToString();

    public KnowledgeItemGroup( KnowledgeKind            kind
                             , IEnumerable<KnowledgeItem> items )
        : base(items)
    {
        Kind = kind;
    }
}
