namespace LocalAIAssistant.Core.BrainDump;

public enum ExtractedItemType
{
    Task
  , Concern
  , Pattern
}

public record ExtractedItem(ExtractedItemType Type, string Description);
