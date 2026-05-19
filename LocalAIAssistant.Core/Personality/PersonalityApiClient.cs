using System.Net.Http.Json;

namespace LocalAIAssistant.Core.Personality;

public class PersonalityApiClient : IPersonalityApiClient
{
    private readonly HttpClient _httpClient;

    private const string PersonalityRoute = "api/personality";

    public PersonalityApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<PersonalityDefinitionDto>> GetPersonalitiesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<PersonalityDefinitionDto>>(PersonalityRoute, ct)
                ?? new List<PersonalityDefinitionDto>();
        }
        catch (Exception)
        {
            return new List<PersonalityDefinitionDto>();
        }
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _httpClient.PutAsync($"{PersonalityRoute}/{id}/activate"
                                     , content: null
                                     , ct);
        }
        catch (Exception)
        {
            // Swallow — callers fire-and-forget; failures logged at call site.
        }
    }
}
