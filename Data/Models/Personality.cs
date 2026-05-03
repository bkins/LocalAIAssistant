using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace LocalAIAssistant.Data.Models;

public partial class Personality : ObservableObject
{
    [ObservableProperty] private Guid         _id = Guid.NewGuid();
    [ObservableProperty] private string       _name;
    [ObservableProperty] private string       _systemPrompt;
    [ObservableProperty] private string?      _tone;    // "Supportive", "Playful", etc.
    [ObservableProperty] private string?      _useCase; // "Motivation", "Debugging", etc.
    [ObservableProperty] private string?      _description;
    [ObservableProperty] private OllamaConfig _ollamConfiguration;

    [ObservableProperty] private string? _voiceId;
    [ObservableProperty] private bool    _isUserGenerated;
    [ObservableProperty] private bool    _isDefault;          // <-- moved here from Persona

    [ObservableProperty] private List<string> _tags;
    [ObservableProperty] private ModelConfig? _modelConfig;
    
    public override string       ToString()  => Name;
    
}