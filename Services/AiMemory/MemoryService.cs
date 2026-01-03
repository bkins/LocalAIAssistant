using System.Text.Json;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Logging;
using Microsoft.Extensions.Options;

namespace LocalAIAssistant.Services.AiMemory;

public class MemoryService : IMemoryService
{
    private readonly Dictionary<string, string> _shortTermMemory = new();
    private readonly string                     _longTermMemoryPath;
    
    private readonly IConversationMemory        _conversationMemory;
    private readonly ILongTermMemoryStore?      _ltm; // optional fallback if not registered

    private static ILoggingService _loggingService;
    
    public MemoryService(IConversationMemory         conversationMemory
                       , IEnumerable<IAiMemoryStore> stores
                       , string                      factsPath
                       , ILoggingService             loggingService)
    {
        _loggingService     = loggingService;
        _conversationMemory = conversationMemory ?? throw new ArgumentNullException(nameof(conversationMemory));
        _ltm                = stores.OfType<ILongTermMemoryStore>().FirstOrDefault();
        _longTermMemoryPath = factsPath ?? throw new ArgumentNullException(nameof(factsPath));
        
        if (!File.Exists(_longTermMemoryPath).Not()) return;
        
        Directory.CreateDirectory(Path.GetDirectoryName(_longTermMemoryPath)!);
        File.WriteAllText(_longTermMemoryPath, "{}");

    }

    // Save an entry (only AI final response in Phase 1)
    public async Task SaveEntryAsync(string role, string content, DateTime utcNow)
    {
        var entry = new Message
                    {
                        Sender    = role
                      , Timestamp = utcNow
                      , Content   = content
                    };

        if (_ltm != null)
        {
            await _ltm.SaveMessageAsync(entry); // delegate to LTM store
        }
    }

    public async Task<MemoryContext> GetContextForTurnAsync(string                           userInput
                                                          , IOptions<MemoryRetrievalOptions> memoryOptions
                                                          , CancellationToken                cancel = default)
    {
        // --- 1) Short-Term Memory (STM) ---
        // var stmPool = _conversationMemory.GetRecentEntries(memoryOptions.Value.MaxStmMessages * 2)
        //                                  .ToList();
        //
        // var stmUsed = stmPool.TakeLast(memoryOptions.Value.MaxStmMessages)
        //                      .ToList();
        
        // --- 1) Short-Term Memory (STM, grouped into turns) ---
        //BUG: If the assistant never replies, then this approach may not work as intended
        var recent = _conversationMemory.GetRecentEntries(memoryOptions.Value.MaxStmMessages * 2)
                                        .OrderBy(message => message.Timestamp)
                                        .ToList();

        var turns       = new List<List<Message>>();
        var currentTurn = new List<Message>();

        foreach (var message in recent)
        {
            currentTurn.Add(message);

            // Heuristic: close the turn when assistant replies
            if (message.Sender != Senders.Assistant) continue;
            
            turns.Add(currentTurn);
            currentTurn = new List<Message>();
        }

        LogTurns(turns);
        
        // Take last N turns (flattened back into a single list)
        var stmUsed = turns.TakeLast(memoryOptions.Value.MaxStmMessages)
                           .SelectMany(messages => messages)
                           .ToList();

        // --- 2) Long-Term Memory (LTM) ---
        IEnumerable<Message> ltmCandidates = Array.Empty<Message>();
        if (_ltm != null)
        {
            var since = DateTime.UtcNow - memoryOptions.Value.LtmRecencyWindow;
            ltmCandidates = await _ltm.GetMessagesSinceAsync(since);
        }
        else
        {
            _loggingService.LogInformation("No LTM");
        }
        
        var factMessages = ltmCandidates.Where(IsUserFact).ToList();
        
        var ltmUsed = RankByKeywordOverlap(ltmCandidates
                                         , userInput
                                         , stmUsed
                                         , memoryOptions.Value.MaxLtmSnippets).ToList();
        
        var merged = factMessages.Concat(ltmUsed)
                                 .GroupBy(message => message.Content) // remove duplicates
                                 .Select(messages => messages.First())
                                 .Take(memoryOptions.Value.MaxLtmSnippets) // still respect cap
                                 .ToList();
        
        foreach (var item in ltmUsed.TakeWhile(item => merged.Count < memoryOptions.Value.MaxLtmSnippets)
                                    .Where(item => merged.Any(message => message.Content == item.Content)
                                                         .Not()))
        {
            merged.Add(item);
        }

        _loggingService.LogInformation("Debug: LTM (merged) messages before BuildSummary:");
        
        foreach (var message in merged)
        {
            _loggingService.LogInformation($"'{message.Content}' Score: {message.Score} Timestamp: {message.Timestamp}", Category.MemoryService);
        }

        // --- 3) Build Summary (delegated to SimpleCompressor) ---
        var summary = SimpleCompressor.BuildSummary(shortTermMemory:   stmUsed
                                                  , longTermMemory:    merged
                                                  , maxChars:          memoryOptions.Value.SummaryMaxChars
                                                  , includeTimestamps: memoryOptions.Value.IncludeTimestamps
                                                  , maxStmItems:       memoryOptions.Value.MaxStmMessages
                                                  , maxLtmItems:       memoryOptions.Value.MaxLtmSnippets
        );
        
        _loggingService.LogInformation("Facts carried into context:");
        foreach (var fact in factMessages)
            _loggingService.LogInformation($"  FACT: {fact.Content}");
        
        // --- 4) Return Context ---
        return new MemoryContext
               {
                   Summary = summary
                 , StmUsed = stmUsed
                 , LtmUsed = merged
               };
    }
    
    private void LogTurns(List<List<Message>> turns)
    {
        _loggingService.LogInformation($"[STM] Grouped into {turns.Count} turns:");

        int turnIndex = 1;
        foreach (var turn in turns)
        {
            _loggingService.LogInformation($"--- Turn {turnIndex} ---");

            foreach (var msg in turn)
            {
                var who  = msg.Sender.ToString();
                var when = msg.Timestamp.ToString("u");
                
                _loggingService.LogInformation($"[{who} @ {when}] {msg.Content}");
            }

            turnIndex++;
        }
    }

    private static bool IsUserFact(Message message)
    {
        if (message.Sender != Senders.User) return false;
        
        var text = message.Content.ToLowerInvariant();

        return text.Contains("my name is")
            || text.Contains("i am")
            || text.Contains("i live")
            || text.Contains("i work")
            || text.Contains("i have")
            || text.Contains("i like")
            || text.Contains("i enjoy");
    }
    public async Task AddMemoryAsync(MemoryType type
                                   , string key
                                   , string value)
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

        var memories = await LoadLongTermMemoryAsync();
        
        return memories.Values;
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
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
    }

    private async Task SaveLongTermMemoryAsync(Dictionary<string, string> memories)
    {
        var json = JsonSerializer.Serialize(memories, new JsonSerializerOptions
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
    public static IEnumerable<Message> RankByKeywordOverlap(IEnumerable<Message> candidates
                                                          , string               userInput
                                                          , IEnumerable<Message> recentMessages
                                                          , int                  take
                                                          , double importanceWeight = 0.5) // tune 0.25..1.0
    {
        var enumerable = candidates.ToList();
        if (enumerable.Count == 0 || take <= 0) return [];

        var queryText = (userInput ?? "") 
                      + " " 
                      + string.Join(" ", recentMessages.Select(m => m.Content ?? ""));
        var hashSet = Tokenize(queryText);

        var ranked = enumerable.Select(message =>
                               {
                                   var overlap = OverlapScore(hashSet, Tokenize(message.Content ?? ""));
                                   message.Score = overlap <= 0 
                                                    ? 0
                                                    : overlap + (message.Importance - 1) * importanceWeight;
                                   return message;
                               })
                               .Where(message => message.Score > 0)
                               .OrderByDescending(message => message.Score)
                               .ThenByDescending(message => message.Timestamp)
                               .Take(take)
                               .ToList();
        
        foreach (var entry in ranked)
        {
            _loggingService.LogInformation($"Ranked Message: '{entry.Content}' Score: {entry.Score:0.##} Importance: {entry.Importance} Timestamp: {entry.Timestamp}");
        }
        
        return ranked; //.Select(candidate => candidate);
    }
    
    public static HashSet<string> Tokenize(string text)
    {
        var parts = (text ?? "").ToLowerInvariant()
                                .Split(new[] { ' ', '\t', '\r', '\n', '.', ',', '!', '?', ':', ';', '(', ')', '[', ']', '{', '}', '-', '_', '"', '\'' }
                                      , StringSplitOptions.RemoveEmptyEntries);
        return new HashSet<string>(parts.Where(part => part.Length <= 48));
    }

    public static int OverlapScore(HashSet<string> leftHash, HashSet<string> rightHash)
    {
        return rightHash.Count(leftHash.Contains);
    }
    public static void ApplyScoring(IEnumerable<Message> messages, string query, IEnumerable<Message> context, double importanceWeight = 0.5)
    {
        var contextTokens = Tokenize(query + " " + string.Join(" ", context.Select(m => m.Content ?? "")));

        foreach (var msg in messages)
        {
            var overlap = OverlapScore(contextTokens, Tokenize(msg.Content ?? ""));
            msg.Score = overlap <= 0
                ? 0
                : overlap + (msg.Importance - 1) * importanceWeight;
        }
    }
}