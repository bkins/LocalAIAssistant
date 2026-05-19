namespace LocalAIAssistant.Core.Personality;

public interface IPersonalityApiClient
{
    Task<IReadOnlyList<PersonalityDefinitionDto>> GetPersonalitiesAsync(CancellationToken ct = default);
    Task ActivateAsync(Guid id, CancellationToken ct = default);
}
