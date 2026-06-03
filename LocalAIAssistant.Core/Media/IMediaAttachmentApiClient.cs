namespace LocalAIAssistant.Core.Media;

public interface IMediaAttachmentApiClient
{
    Task<MediaAttachmentDto?>               UploadAsync  (Guid              journalId
                                                        , string            fileName
                                                        , string            contentType
                                                        , Stream            stream
                                                        , CancellationToken ct = default);

    Task<IReadOnlyList<MediaAttachmentDto>?> ListAsync   (Guid              journalId
                                                        , CancellationToken ct = default);

    Task<bool>                              DeleteAsync  (Guid              id
                                                        , CancellationToken ct = default);

    Task<Stream?>                           DownloadAsync(Guid              id
                                                        , CancellationToken ct = default);
}
