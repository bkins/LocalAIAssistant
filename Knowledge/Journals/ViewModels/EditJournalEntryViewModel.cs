using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.Knowledge.Journals.ViewModels;

public sealed partial class EditJournalEntryViewModel : ObservableObject
                                                      , IQueryAttributable
{
    private readonly IJournalApiClientFactory _clientFactory;

    private Guid _journalId;

    public EditJournalEntryViewModel(IJournalApiClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    // Editable fields (pre-populated)
    public string Text { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string? Mood { get; set; }
    public int? MoodScore { get; set; }

    public bool IsLoading { get; private set; }
    public bool HasError   { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj)
            && Guid.TryParse(idObj?.ToString(), out var id))
        {
            _journalId = id;
        }
    }

    public async Task LoadAsync()
    {
        if (_journalId == Guid.Empty) return;

        IsLoading = true;
        OnPropertyChanged(nameof(IsLoading));

        var client = _clientFactory.Create();
        var entry  = await client.GetByIdAsync(_journalId);

        // Pre-populate from latest revision
        Text      = entry?.Text ?? string.Empty;
        Mood      = entry?.Mood;
        MoodScore = entry?.MoodScore;
        Tags = entry?.Tags is { Count: > 0 }
                       ? string.Join(", ", entry.Tags)
                       : string.Empty;

        SetDtoError(entry);
        
        OnPropertyChanged(string.Empty); // refresh all bindings
        IsLoading = false;
        OnPropertyChanged(nameof(IsLoading));
    }

    private void SetDtoError( JournalEntryDto? entry )
    {
        if (entry?.Error is null) return;
        
        HasError     = true;
        ErrorMessage = entry.Error.Message;
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        var client = _clientFactory.Create();
        
        await client.EditEntryAsync(_journalId
                                 , Text
                                 , ParseTags(Tags)
                                 , Mood
                                 , MoodScore);

        await Shell.Current.GoToAsync(".."); // back to detail
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private static IReadOnlyList<string>? ParseTags(string tags)
    {
        return string.IsNullOrWhiteSpace(tags)
                       ? null
                       : tags.Split(',')
                             .Select(tag => tag.Trim())
                             .Where(tag => tag.Length > 0)
                             .ToList();
    }
}
