namespace LocalAIAssistant.Core.Personalities;

public class PersonalityRecord
{
    public Guid         Id           { get; set; } = Guid.NewGuid();
    public string       Name         { get; set; } = string.Empty;
    public string?      Description  { get; set; }
    public string?      SystemPrompt { get; set; }
    public bool         IsDefault    { get; set; }
    public string?      Tone         { get; set; }
    public string?      UseCase      { get; set; }
    public string?      VoiceId      { get; set; }
    public List<string> Tags         { get; set; } = new();

    public PersonalityModelConfig? ModelConfig { get; set; }
}
