using LocalAIAssistant.Core.Environment.Models;

namespace LocalAIAssistant.Core.Environment;

public sealed class EnvironmentHandshakeState
{
    public EnvironmentHandshakeResult Current { get; private set; } =
        new(
            "UNKNOWN"
          , "UNKNOWN"
          , HandshakeSeverity.Info
          , AllowWrites: true
          , UserMessage: "Handshake not yet performed."
          , MoreDetails: "N/A"
        );

    public void Set(EnvironmentHandshakeResult result) => Current = result;
}