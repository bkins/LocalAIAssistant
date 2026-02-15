namespace LocalAIAssistant.Core.Environment.Models;

public enum HandshakeSeverity
{
    None
  , Info       // DEV mismatch
  , Warning    // QA mismatch
  , Restricted // PROD involved
}

public sealed record EnvironmentHandshakeResult
(
    string            ClientEnvironment
  , string            ApiEnvironment
  , HandshakeSeverity Severity
  , bool              AllowWrites
  , string            UserMessage
)
{
    public bool HasMismatch => !string.Equals(ClientEnvironment
                                            , ApiEnvironment
                                            , StringComparison.OrdinalIgnoreCase);
}