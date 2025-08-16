using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.Services;

public class RoleInjectionService : IRoleInjectionService
{
    private readonly IMemoryService _memoryService;

    public RoleInjectionService(IMemoryService memoryService)
    {
        _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
    }

    /// <summary>
    /// Builds a role prompt by combining a base description with optional short-term
    /// and long-term memory retrieved from the memory service.
    /// </summary>
    public async Task<string> BuildRolePromptAsync(string baseRoleDescription
                                                 , bool   includeShortTermMemory
                                                 , bool   includeLongTermMemory)
    {
        if (string.IsNullOrWhiteSpace(baseRoleDescription))
            throw new ArgumentException("Base role description cannot be null or whitespace."
                                      , nameof(baseRoleDescription));

        string prompt = baseRoleDescription;

        if (includeShortTermMemory)
        {
            var stmEntries = await _memoryService.GetAllMemoriesAsync(MemoryType.ShortTerm);
            if (stmEntries.Any())
            {
                prompt += "\n\n[Short-Term Memory]\n"
                        + string.Join("\n"
                                    , stmEntries);
            }
        }

        if (includeLongTermMemory)
        {
            var ltmEntries = await _memoryService.GetAllMemoriesAsync(MemoryType.LongTerm);
            if (ltmEntries.Any())
            {
                prompt += "\n\n[Long-Term Memory]\n"
                        + string.Join("\n"
                                    , ltmEntries);
            }
        }

        return prompt;
    }

    /// <summary>
    /// Combines a base system prompt, persona name, and contextual summary into the final system message for the AI.
    /// </summary>
    public string BuildInjectedSystemPrompt(
        string baseSystemPrompt,
        string personaName,
        string contextSummary)
    {
        if (string.IsNullOrWhiteSpace(baseSystemPrompt))
            throw new ArgumentException("Base system prompt cannot be null or whitespace.", nameof(baseSystemPrompt));

        var systemPrompt = $"{baseSystemPrompt}\n\nYou are now roleplaying as '{personaName}'.\nStay in character at all times.";

        if (!string.IsNullOrWhiteSpace(contextSummary))
            systemPrompt += $"\n\n[Current Context]\n{contextSummary}";

        return systemPrompt;
    }
}
