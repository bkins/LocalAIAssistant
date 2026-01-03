namespace LocalAIAssistant.Views.Helpers;

public static class KeyboardCreator
{
    public static Keyboard CreateKeyboard { get; } =
        Keyboard.Create(KeyboardFlags.CapitalizeSentence | KeyboardFlags.Suggestions);
}
