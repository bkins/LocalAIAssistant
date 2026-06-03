using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Core.Media;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.Knowledge.Journals.ViewModels;

public sealed partial class EditJournalEntryViewModel : ObservableObject
                                                      , IQueryAttributable
{
    private readonly IJournalApiClientFactory   _clientFactory;
    private readonly IMediaAttachmentApiClient  _mediaClient;

    private Guid _journalId;

    public EditJournalEntryViewModel( IJournalApiClientFactory  clientFactory
                                    , IMediaAttachmentApiClient mediaClient )
    {
        _clientFactory = clientFactory;
        _mediaClient   = mediaClient;
    }

    // Editable fields (pre-populated)
    public string Text { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string? Mood { get; set; }
    public int? MoodScore { get; set; }

    public bool IsLoading { get; private set; }
    public bool HasError   { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;

    public ObservableCollection<AttachmentViewModel> Attachments { get; } = new();

    public bool HasAttachments => Attachments.Count > 0;

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

        Text      = entry?.Text ?? string.Empty;
        Mood      = entry?.Mood;
        MoodScore = entry?.MoodScore;
        Tags = entry?.Tags is { Count: > 0 }
                       ? string.Join(", ", entry.Tags)
                       : string.Empty;

        SetDtoError(entry);

        await LoadAttachmentsAsync();

        OnPropertyChanged(string.Empty);
        IsLoading = false;
        OnPropertyChanged(nameof(IsLoading));
    }

    private async Task LoadAttachmentsAsync()
    {
        var list = await _mediaClient.ListAsync(_journalId);
        Attachments.Clear();

        if (list is null) return;

        foreach (var attachment in list)
            Attachments.Add(new AttachmentViewModel(attachment, BuildEnvironment.ApiBaseUrl, DeleteAttachmentAsync));

        OnPropertyChanged(nameof(HasAttachments));
    }

    [RelayCommand]
    private async Task AttachFromGalleryAsync()
    {
        if (_journalId == Guid.Empty) return;

        FileResult? photo = null;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                photo = await MediaPicker.Default.PickPhotoAsync();
            });
        }
        catch (Exception)
        {
            return;
        }

        if (photo is null) return;
        await UploadPickedFileAsync(photo);
    }

    [RelayCommand]
    private async Task AttachFromCameraAsync()
    {
        if (_journalId == Guid.Empty) return;

        FileResult? photo = null;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                photo = await MediaPicker.Default.CapturePhotoAsync();
            });
        }
        catch (FeatureNotSupportedException)
        {
            return;
        }
        catch (Exception)
        {
            return;
        }

        if (photo is null) return;
        await UploadPickedFileAsync(photo);
    }

    private async Task UploadPickedFileAsync(FileResult fileResult)
    {
        await using var stream      = await fileResult.OpenReadAsync();
        var             contentType = fileResult.ContentType ?? "application/octet-stream";

        var dto = await _mediaClient.UploadAsync(_journalId
                                               , fileResult.FileName
                                               , contentType
                                               , stream);
        if (dto is null) return;

        Attachments.Add(new AttachmentViewModel(dto, BuildEnvironment.ApiBaseUrl, DeleteAttachmentAsync));
        OnPropertyChanged(nameof(HasAttachments));
    }

    private async Task DeleteAttachmentAsync(Guid id)
    {
        var success = await _mediaClient.DeleteAsync(id);
        if (!success) return;

        var toRemove = Attachments.FirstOrDefault(chip => chip.Id == id);
        if (toRemove is not null)
        {
            Attachments.Remove(toRemove);
            OnPropertyChanged(nameof(HasAttachments));
        }
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

        await Shell.Current.GoToAsync("..");
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
