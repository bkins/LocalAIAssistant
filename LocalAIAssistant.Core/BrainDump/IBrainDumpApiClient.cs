namespace LocalAIAssistant.Core.BrainDump;

public interface IBrainDumpApiClient
{
    Task<BrainDumpSessionDto>  StartSessionAsync    (CancellationToken ct = default);

    Task<BrainDumpSessionDto?> GetSessionAsync      (string                id
                                                    , CancellationToken    ct = default);

    Task<BrainDumpSessionDto?> UpdateCategoryAsync  (string                id
                                                    , BrainDumpCategoryField field
                                                    , string               text
                                                    , CancellationToken    ct = default);

    Task<BrainDumpSessionDto?> MarkProcessedAsync   (string                id
                                                    , string?              summary
                                                    , IReadOnlyList<string> taskIds
                                                    , CancellationToken    ct = default);
}
