using CommunityToolkit.Mvvm.ComponentModel;

namespace LocalAIAssistant.Data.Models;

public partial class Personality : ObservableObject
{

    [ObservableProperty] private string  _name;
    [ObservableProperty] public  string  _systemPrompt;
    [ObservableProperty] public  string? _tone; // e.g., "Supportive", "Playful", "Professional", etc.
    [ObservableProperty] public  string? _useCase; // e.g., "Motivation", "Debugging", "Wellness"
    [ObservableProperty] public  string? _description;

    // Useful later
    [ObservableProperty] public string? _voiceId;
    [ObservableProperty] public bool    _isUserGenerated;

    public override string ToString() => Name;
    /*
     * For Example:
     *
        new Personality
        {
            Name = "Code Mentor",
            Description = "Helpful C# coding assistant",
            SystemPrompt = "You are an expert C# developer helping a junior programmer.",
            VoiceId = "mentor"
        }
     */

}