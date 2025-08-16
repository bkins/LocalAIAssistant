using System.Text.Json;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.AiMemory.Interfaces;

namespace LocalAIAssistant.Services.AiMemory;

public class MemoryService : IMemoryService
{
    private readonly Dictionary<string, string> _shortTermMemory = new();
    private readonly string                     _longTermMemoryPath;
    private readonly string                     _filePath;
    
    private readonly IConversationMemory        _conversationMemory;
    private readonly ILongTermMemoryStore?      _ltm; // optional fallback if not registered

    public MemoryService(IConversationMemory         conversationMemory
                       , IEnumerable<IAiMemoryStore> stores
                       , string                      factsPath
                       , string                      filePath)
    {
        _conversationMemory = conversationMemory ?? throw new ArgumentNullException(nameof(conversationMemory));
        _ltm                = stores.OfType<ILongTermMemoryStore>().FirstOrDefault();

        _longTermMemoryPath = factsPath ?? throw new ArgumentNullException(nameof(factsPath));
        if (!File.Exists(_longTermMemoryPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_longTermMemoryPath)!);
            File.WriteAllText(_longTermMemoryPath
                            , "{}");
        }

        _filePath = filePath;
    }

    // Save an entry (only AI final response in Phase 1)
    public async Task SaveEntryAsync(string   role
                                   , string   content
                                   , DateTime utcNow)
    {
        var entry = new MemoryEntry
                    {
                        Role      = role
                      , Timestamp = DateTime.UtcNow
                      , Content   = content
                    };

        var json = JsonSerializer.Serialize(entry);
        await File.AppendAllTextAsync(_filePath, json + Environment.NewLine);
    }
    
    public async Task<List<MemoryEntry>> LoadEntriesAsync()
    {
        if (!File.Exists(_filePath))
            return new List<MemoryEntry>();

        var lines = await File.ReadAllLinesAsync(_filePath);
        return lines.Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => JsonSerializer.Deserialize<MemoryEntry>(line))
                    .Where(entry => entry != null)
                    .ToList()!;
    }
    
    public async Task<MemoryContext> GetContextForTurnAsync(string userInput
                                                          , MemoryRetrievalOptions opts
                                                          , CancellationToken ct = default)
    {
        // 1) STM: take a bit more, then trim to target
        var stmPool = _conversationMemory.GetRecentEntries(opts.MaxStmMessages * 2).ToList();
        var stmUsed = stmPool.TakeLast(opts.MaxStmMessages).ToList();

        // 2) LTM: naive keyword match within a recency window (if LTM exists)
        IEnumerable<Message> ltmCandidates = Array.Empty<Message>();
        if (_ltm != null)
        {
            var since = DateTime.UtcNow - opts.LtmRecencyWindow;
            ltmCandidates = await _ltm.GetMessagesSinceAsync(since);
        }

        var ltmUsed = RankByKeywordOverlap(ltmCandidates, userInput, stmUsed, opts.MaxLtmSnippets).ToList();

        // 3) Compress
        var summary = SimpleCompressor.BuildSummary(stmUsed, ltmUsed, opts.SummaryMaxChars, opts.IncludeTimestamps);

        return new MemoryContext
               {
                   Summary = summary,
                   StmUsed = stmUsed,
                   LtmUsed = ltmUsed
               };
    }
    public async Task AddMemoryAsync(MemoryType type, string key, string value)
    {
        if (type == MemoryType.ShortTerm)
        {
            _shortTermMemory[key] = value;
        }
        else
        {
            var memories = await LoadLongTermMemoryAsync();
            memories[key] = value;
            await SaveLongTermMemoryAsync(memories);
        }
    }

    public async Task<string?> GetMemoryAsync(MemoryType type, string key)
    {
        if (type == MemoryType.ShortTerm)
        {
            return _shortTermMemory.TryGetValue(key, out var value) ? value : null;
        }
        else
        {
            var memories = await LoadLongTermMemoryAsync();
            return memories.TryGetValue(key, out var value) ? value : null;
        }
    }

    public async Task<IEnumerable<string>> GetAllMemoriesAsync(MemoryType type)
    {
        if (type == MemoryType.ShortTerm)
        {
            return _shortTermMemory.Values;
        }
        else
        {
            var memories = await LoadLongTermMemoryAsync();
            return memories.Values;
        }
    }

    public Task RemoveMemoryAsync(MemoryType type, string key)
    {
        if (type == MemoryType.ShortTerm)
        {
            _shortTermMemory.Remove(key);
        }
        else
        {
            return RemoveLongTermMemoryAsync(key);
        }

        return Task.CompletedTask;
    }

    public Task ClearMemoryAsync(MemoryType type)
    {
        if (type == MemoryType.ShortTerm)
        {
            _shortTermMemory.Clear();
        }
        else
        {
            File.WriteAllText(_longTermMemoryPath, "{}");
        }

        return Task.CompletedTask;
    }

    public void StoreMemory(string role
                          , string content
                          , DateTime timestamp
                          , double importance = 0.5)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<MemoryEntry> RetrieveRelevantMemories(string query
                                                           , int maxResults = 5) => throw new NotImplementedException();

    public void ForgetMemories(Func<MemoryEntry, bool> predicate)
    {
        throw new NotImplementedException();
    }
    
    private async Task<Dictionary<string, string>> LoadLongTermMemoryAsync()
    {
        var json = await File.ReadAllTextAsync(_longTermMemoryPath);
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
    }

    private async Task SaveLongTermMemoryAsync(Dictionary<string, string> memories)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(memories, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_longTermMemoryPath, json);
    }

    private async Task RemoveLongTermMemoryAsync(string key)
    {
        var memories = await LoadLongTermMemoryAsync();
        if (memories.Remove(key))
        {
            await SaveLongTermMemoryAsync(memories);
        }
    }
    // ---- helpers ----
    private static IEnumerable<Message> RankByKeywordOverlap(IEnumerable<Message> candidates, string userInput, IEnumerable<Message> recent, int take)
    {
        if (!candidates.Any() || take <= 0) return Enumerable.Empty<Message>();

        var queryText = (userInput ?? "") + " " + string.Join(" ", recent.Select(m => m.Content ?? ""));
        var q = Tokenize(queryText);

        return candidates
               .Select(m => new { Msg = m, Score = OverlapScore(q, Tokenize(m.Content ?? "")) })
               .Where(x => x.Score > 0)
               .OrderByDescending(x => x.Score)
               .ThenBy(x => x.Msg.Timestamp)
               .Take(take)
               .Select(x => x.Msg);
    }

    private static HashSet<string> Tokenize(string text)
    {
        var parts = (text ?? "").ToLowerInvariant()
                                .Split(new[] { ' ', '\t', '\r', '\n', '.', ',', '!', '?', ':', ';', '(', ')', '[', ']', '{', '}', '-', '_', '"', '\'' },
                                       StringSplitOptions.RemoveEmptyEntries);
        return new HashSet<string>(parts.Where(p => p.Length <= 48));
    }

    private static int OverlapScore(HashSet<string> a, HashSet<string> b) => b.Count(a.Contains);

}