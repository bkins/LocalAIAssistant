using LocalAIAssistant.Core.Environment.Models;

namespace LocalAIAssistant.Core.Environment;

public sealed class EnvironmentHandshakeState
{
    public EnvironmentHandshakeResult Current { get; private set; } =
        new
                (
                    "UNKNOWN"
                  , "UNKNOWN"
                  , HandshakeSeverity.Info
                  , AllowWrites: true
                  , "Handshake not yet performed."
                );

    public void Set(EnvironmentHandshakeResult result) => Current = result;
}