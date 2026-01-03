using LocalAIAssistant.PersonaAndContextEngine.Models;

namespace LocalAIAssistant.PersonaAndContextEngine.Interfaces;

public interface IPersonaAndContextEngine
{

    Task<PersonaContextResult> ResolveContextAsync(string            userInput
                                                 , Guid?             forcedPersonaId = null,
                                                   CancellationToken ct              = default);

}