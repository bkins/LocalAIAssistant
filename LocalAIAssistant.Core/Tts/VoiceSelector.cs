namespace LocalAIAssistant.Core.Tts;

public static class VoiceSelector
{
    /// <summary>
    /// Returns the best-matching voice from <paramref name="voices"/> given an optional
    /// preferred name and an optional BCP-47 language code.
    /// Priority: exact name match → language match → first voice → null when empty.
    /// </summary>
    public static VoiceInfo? SelectVoice( IReadOnlyList<VoiceInfo> voices
                                        , string?                  preferredName
                                        , string?                  languageCode = null )
    {
        if (voices.Count == 0)
            return null;

        if (preferredName.HasValue())
        {
            var match = voices.FirstOrDefault(voice =>
                voice.Name.EqualsIgnoreCase(preferredName));

            if (match is not null)
                return match;
        }

        if (languageCode.HasValue())
        {
            var langMatch = voices.FirstOrDefault(voice =>
                voice.Language.EqualsIgnoreCase(languageCode));

            if (langMatch is not null)
                return langMatch;
        }

        return voices[0];
    }
}
