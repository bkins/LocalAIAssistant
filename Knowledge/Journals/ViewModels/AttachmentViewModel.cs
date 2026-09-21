using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Core.Media;

namespace LocalAIAssistant.Knowledge.Journals.ViewModels;

public sealed class AttachmentViewModel
{
    public Guid              Id           { get; }
    public string            FileName     { get; }
    public string StoragePath    { get; }
    public bool              IsImage      { get; }
    public string            RemoveAccessibilityText => $"Remove attachment {FileName}";
    public IAsyncRelayCommand DeleteCommand { get; }
    public IAsyncRelayCommand OpenCommand   { get; }

    public AttachmentViewModel( MediaAttachmentDto  dto
                              , string              baseUrl
                              , Func<Guid, Task>    onDelete
                              , Func<string, Task<bool>>? confirmDelete = null )
    {
        Id           = dto.Id;
        FileName     = dto.FileName;
        StoragePath  = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), $"api/media/{dto.Id}/file").AbsoluteUri;
        IsImage      = dto.IsImage;

        DeleteCommand = new AsyncRelayCommand(async () =>
        {
            if (confirmDelete is not null && !await confirmDelete(dto.FileName)) return;
            await onDelete(dto.Id);
        });
        OpenCommand   = new AsyncRelayCommand(OpenAsync);
    }

    private async Task OpenAsync()
    {
        try
        {
            await Launcher.OpenAsync(StoragePath);
        }
        catch (Exception)
        {
            // Launcher failure is non-fatal — media might not be previewable on this device.
        }
    }
}
