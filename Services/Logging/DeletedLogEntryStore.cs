using System.Collections.Concurrent;
using System.Text;

namespace LocalAIAssistant.Services.Logging;

public sealed class DeletedLogEntryStore
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _deletionFilePath;
    private readonly SemaphoreSlim _gate;

    public DeletedLogEntryStore(string deletionFilePath)
    {
        _deletionFilePath = deletionFilePath;
        _gate = Gates.GetOrAdd(deletionFilePath, static _ => new SemaphoreSlim(1, 1));
    }

    public async Task<HashSet<string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadAllAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> AddAsync( string            logFilePath
                                    , string            storageId
                                    , long              offset
                                    , string            expectedText
                                    , CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageId) || !File.Exists(logFilePath)) return false;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var deletedIds = await ReadAllAsync(cancellationToken);
            if (deletedIds.Contains(storageId)) return false;

            var currentRecord = await Task.Run(() => NewestLogLineReader.ReadRecordAtOffset(logFilePath, offset, cancellationToken)
                                             , cancellationToken);
            if (currentRecord is null
             || !currentRecord.Text.Equals(expectedText, StringComparison.Ordinal)
             || !currentRecord.StorageId.Equals(storageId, StringComparison.OrdinalIgnoreCase)) return false;

            await File.AppendAllTextAsync(_deletionFilePath
                                        , storageId + Environment.NewLine
                                        , new UTF8Encoding(false)
                                        , cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_deletionFilePath))
                await File.WriteAllTextAsync(_deletionFilePath, string.Empty, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<HashSet<string>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_deletionFilePath)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var lines = await File.ReadAllLinesAsync(_deletionFilePath, cancellationToken);
        return lines.Where(identity => !string.IsNullOrWhiteSpace(identity))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
