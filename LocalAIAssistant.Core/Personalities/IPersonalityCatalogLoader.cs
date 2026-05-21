namespace LocalAIAssistant.Core.Personalities;

public interface IPersonalityCatalogLoader
{
    List<PersonalityRecord> Load();
}
