namespace LocalAIAssistant.Services.Interfaces;

public interface IRoleInjectionService
{
    Task<string> BuildRolePromptAsync(string      baseRoleDescription, bool   includeShortTermMemory, bool   includeLongTermMemory);
    string       BuildInjectedSystemPrompt(string baseSystemPrompt,    string personaName,            string contextSummary);
}