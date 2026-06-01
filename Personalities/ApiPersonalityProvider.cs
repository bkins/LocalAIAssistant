using LocalAIAssistant.Core.Personality;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Personalities;

public class ApiPersonalityProvider : IPersonalityProvider
{
    private readonly IPersonalityApiClient _apiClient;

    public ApiPersonalityProvider(IPersonalityApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public IEnumerable<Personality> Load()
    {
        try
        {
            // Without a timeout this blocks the main thread (during DI construction) for up to the 100-second HttpClient default.
            using var cts  = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var       dtos = Task.Run(() => _apiClient.GetPersonalitiesAsync(cts.Token)).GetAwaiter().GetResult();

            return dtos.Select(Map).ToList();
        }
        catch (Exception)
        {
            return Enumerable.Empty<Personality>();
        }
    }

    private static Personality Map(PersonalityDefinitionDto dto)
    {
        var ollamaConfig = dto.ModelConfig == null
                               ? null
                               : new OllamaConfig
                                 {
                                       Host        = StringConsts.OllamaServerUrl
                                     , Model       = dto.ModelConfig.ModelId
                                     , Temperature = dto.ModelConfig.Temperature
                                     , NumPredict  = 256
                                 };

        return new Personality
               {
                       Id                 = dto.Id
                     , Name               = dto.Name
                     , Description        = dto.Description
                     , SystemPrompt       = dto.SystemPrompt ?? string.Empty
                     , IsDefault          = dto.IsActive
                     , IsUserGenerated    = !dto.IsBuiltIn
                     , OllamConfiguration = ollamaConfig
                     , ModelConfig        = dto.ModelConfig == null
                                               ? null
                                               : new ModelConfig
                                                 {
                                                       Model       = dto.ModelConfig.ModelId
                                                     , Temperature = dto.ModelConfig.Temperature
                                                 }
               };
    }
}
