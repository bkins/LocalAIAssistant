using LocalAIAssistant.Data;
using LocalAIAssistant.Services.Interfaces;
using SpeechSdk = Microsoft.CognitiveServices.Speech;

namespace LocalAIAssistant.Services;

public sealed class AzureSpeechToTextService : ISpeechToTextService
{
    private static string? ActiveKey
        => Preferences.Default.Get(StringConsts.TtsAzureKeyPrefKey, string.Empty) is { Length: > 0 } key
               ? key
               : null;

    private static string ActiveRegion
        => Preferences.Default.Get(StringConsts.TtsAzureRegionPrefKey, "eastus");

    public bool IsAvailable => !string.IsNullOrEmpty(ActiveKey);

    public async Task<string?> RecognizeSpeechAsync(CancellationToken cancellationToken = default)
    {
        var key = ActiveKey;
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        // Request microphone permission at runtime
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Microphone>();
        }

        if (status != PermissionStatus.Granted)
        {
            return null;
        }

        try
        {
            var config = SpeechSdk.SpeechConfig.FromSubscription(key, ActiveRegion);
            using var recognizer = new SpeechSdk.SpeechRecognizer(config);

            using var cancelRegistration = cancellationToken.Register(() =>
            {
                _ = recognizer.StopContinuousRecognitionAsync();
            });

            var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

            if (result.Reason == SpeechSdk.ResultReason.RecognizedSpeech)
            {
                return result.Text;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
