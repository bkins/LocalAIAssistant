namespace LocalAIAssistant.Core.Media;

public sealed class MediaAttachmentDto
{
    public Guid           Id          { get; init; }
    public string         FileName    { get; init; } = string.Empty;
    public string         ContentType { get; init; } = string.Empty;
    public long           FileSize    { get; init; }
    public DateTimeOffset CreatedAt   { get; init; }
    public string         OwnerType   { get; init; } = string.Empty;
    public Guid           OwnerId     { get; init; }
    public string         StoragePath { get; init; } = string.Empty;

    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
