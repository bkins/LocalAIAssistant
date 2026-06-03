using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Core.Media;

namespace LocalAIAssistant.Knowledge.Journals.ViewModels;

public sealed class AttachmentViewModel
{
    public Guid              Id           { get; }
    public string            FileName     { get; }
    public string            ThumbnailUrl { get; }
    public bool              IsImage      { get; }
    public IAsyncRelayCommand DeleteCommand { get; }
    public IAsyncRelayCommand OpenCommand   { get; }

    public AttachmentViewModel( MediaAttachmentDto  dto
                              , string              baseUrl
                              , Func<Guid, Task>    onDelete )
    {
        Id           = dto.Id;
        FileName     = dto.FileName;
        ThumbnailUrl = $"{baseUrl.TrimEnd('/')}/api/media/{dto.Id}/file";
        IsImage      = dto.IsImage;

        DeleteCommand = new AsyncRelayCommand(() => onDelete(dto.Id));
        OpenCommand   = new AsyncRelayCommand(OpenAsync);
    }

    private async Task OpenAsync()
    {
        try
        {
            await Launcher.OpenAsync(new Uri(ThumbnailUrl));
        }
        catch (Exception)
        {
            // Launcher failure is non-fatal — media might not be previewable on this device.
        }
    }
}
