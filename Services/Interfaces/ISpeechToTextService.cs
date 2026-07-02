using System.Threading;
using System.Threading.Tasks;

namespace LocalAIAssistant.Services.Interfaces;

public interface ISpeechToTextService
{
    /// <summary>
    /// Gets whether the speech-to-text service is currently configured and available to use (e.g. key present).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Starts speech recognition and returns the transcribed text. Returns null if canceled, failed, or permission denied.
    /// </summary>
    Task<string?> RecognizeSpeechAsync(CancellationToken cancellationToken = default);
}
