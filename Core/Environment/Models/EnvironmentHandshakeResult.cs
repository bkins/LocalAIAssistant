namespace LocalAIAssistant.Core.Environment.Models;

public enum HandshakeSeverity
{
    None
  , Info       // DEV mismatch
  , Warning    // QA mismatch
  , Restricted // PROD involved
  , Error
}

public sealed record EnvironmentHandshakeResult( string            ClientEnvironment
                                               , string            ApiEnvironment
                                               , HandshakeSeverity Severity
                                               , bool              AllowWrites
                                               , string            UserMessage
                                               , string            MoreDetails = "N/A")
{
    public bool HasMismatch => !string.Equals(ClientEnvironment
                                            , ApiEnvironment
                                            , StringComparison.OrdinalIgnoreCase);

    
}