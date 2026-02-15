namespace LocalAIAssistant.Core.Environment;

public sealed class WriteGuard
{
    private readonly EnvironmentHandshakeState _state;

    public WriteGuard(EnvironmentHandshakeState state) => _state = state;

    public bool CanWrite(out string reason)
    {
        var current = _state.Current;

        if (current.AllowWrites)
        {
            reason = "";
            return true;
        }

        reason = current.UserMessage;
        return false;
    }
}