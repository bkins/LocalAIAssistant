using LocalAIAssistant.Core.Personality;
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
            var dtos = Task.Run(() => _apiClient.GetPersonalitiesAsync()).GetAwaiter().GetResult();

            return dtos.Select(Map).ToList();
        }
        catch (Exception)
        {
            return Enumerable.Empty<Personality>();
        }
    }

    private static Personality Map(PersonalityDefinitionDto dto)
        => new()
           {
                   Id              = dto.Id
                 , Name            = dto.Name
                 , Description     = dto.Description
                 , SystemPrompt    = dto.SystemPrompt ?? string.Empty
                 , IsDefault       = dto.IsActive
                 , IsUserGenerated = !dto.IsBuiltIn
                 , ModelConfig     = dto.ModelConfig == null
                                         ? null
                                         : new ModelConfig
                                           {
                                                   Model       = dto.ModelConfig.ModelId
                                                 , Temperature = dto.ModelConfig.Temperature
                                           }
           };
}
