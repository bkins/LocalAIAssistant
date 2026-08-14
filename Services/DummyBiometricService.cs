using System.Threading.Tasks;

namespace LocalAIAssistant.Services;

public sealed class DummyBiometricService : IBiometricService
{
    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(false);
    }

    public Task<bool> AuthenticateAsync(string reason)
    {
        return Task.FromResult(false);
    }
}
