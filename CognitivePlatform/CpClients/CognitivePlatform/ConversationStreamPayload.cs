using System.Text.Json;

namespace LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

public static class ConversationStreamPayload
{
    public const string HeaderName = "X-CP-Stream-Format";
    public const string JsonStringFormat = "json-string-v1";

    public static string Decode(string payload, bool jsonStrings)
        => jsonStrings
               ? JsonSerializer.Deserialize<string>(payload) ?? throw new JsonException("Null conversation chunk.")
               : payload;
}
