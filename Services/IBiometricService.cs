using System.Threading.Tasks;

namespace LocalAIAssistant.Services;

public interface IBiometricService
{
    Task<bool> IsAvailableAsync();
    Task<bool> AuthenticateAsync(string reason);
}
