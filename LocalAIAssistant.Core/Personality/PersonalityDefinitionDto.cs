namespace LocalAIAssistant.Core.Personality;

public class PersonalityDefinitionDto
{
    public Guid    Id           { get; init; }
    public string  Name         { get; init; } = string.Empty;
    public string? Description  { get; init; }
    public string? SystemPrompt { get; init; }
    public bool    IsBuiltIn    { get; init; }
    public bool    IsActive     { get; init; }

    public PersonalityModelConfigDto? ModelConfig { get; init; }
}

public class PersonalityModelConfigDto
{
    public string? Provider             { get; init; }
    public string? ModelId              { get; init; }
    public string? SystemPromptOverride { get; init; }
    public float   Temperature          { get; init; } = 0.7f;
}
