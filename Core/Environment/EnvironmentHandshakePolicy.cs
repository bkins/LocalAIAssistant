using LocalAIAssistant.Core.Environment.Models;
using LocalAIAssistant.Extensions;

namespace LocalAIAssistant.Core.Environment;

public static class EnvironmentHandshakePolicy
{
    public static EnvironmentHandshakeResult Evaluate(string clientEnv, string apiEnv)
    {
        clientEnv = Normalize(clientEnv);
        apiEnv    = Normalize(apiEnv);

        if (clientEnv == apiEnv)
        {
            return new EnvironmentHandshakeResult(clientEnv
                                                , apiEnv
                                                , HandshakeSeverity.None
                                                , AllowWrites: true
                                                , UserMessage: "Connected to matching environment.");
        }

        var involvesProd = clientEnv == "PROD" || apiEnv == "PROD";
        var involvesQa   = clientEnv == "QA"   || apiEnv == "QA";
        // If mismatch and not prod/qa-involved, it’s a DEV-ish mismatch.
        // (Ex: DEV vs something else, or unknown labels)

        if (involvesProd)
        {
            return new EnvironmentHandshakeResult(clientEnv
                                                , apiEnv
                                                , HandshakeSeverity.Restricted
                                                , AllowWrites: false
                                                , UserMessage: $"Environment mismatch: App is '{clientEnv}' but API is '{apiEnv}'. "
                                                             + "Writes are disabled to protect production data.");
        }

        if (involvesQa)
        {
            return new EnvironmentHandshakeResult(clientEnv
                                                , apiEnv
                                                , HandshakeSeverity.Warning
                                                , AllowWrites: true
                                                , UserMessage: $"Environment mismatch: App is '{clientEnv}' but API is '{apiEnv}'. "
                                                             + "Proceed with caution."
            );
        }

        // DEV mismatch (or anything else)
        return new EnvironmentHandshakeResult(clientEnv
                                            , apiEnv
                                            , HandshakeSeverity.Info
                                            , AllowWrites: true
                                            , UserMessage: $"Environment mismatch: App is '{clientEnv}' but API is '{apiEnv}'. "
                                                         + "Continuing normally (DEV-level notice).");
    }

    private static string Normalize(string env) => env.HasNoValue() 
                                                           ? "UNKNOWN" 
                                                           : env.Trim().ToUpperInvariant();
}