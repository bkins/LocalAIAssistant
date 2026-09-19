using System.Security.Cryptography;
using System.Text;

namespace LocalAIAssistant.Services.Logging;

public static class LogEntryIdentity
{
    public static string Create( long   offset
                               , string text)
    {
        var value = $"{offset}:{text}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
