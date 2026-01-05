#if DEBUG
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.Logging;

namespace LocalAIAssistant.Debugging;

public class TestHarness
{
    private readonly ILoggingService _logger;

    public TestHarness(ILoggingService logger)
    {
        _logger = logger;
    }

    public void RunAll()
    {
        _logger.LogInformation("===== TestHarness Starting =====");

        RunRankByKeywordOverlapTest();
        RunBuildSummaryTest();
        RunBuildSummaryTest_WithImportance();
        
        _logger.LogInformation("===== TestHarness Finished =====");
    }

    private void RunRankByKeywordOverlapTest()
    {
        _logger.LogInformation("Running RankByKeywordOverlap test...");

        var messages = new List<Message>
                       {
                               new()
                               {
                                       Sender    = Senders.Ai
                                     , Content   = "The sky is blue"
                                     , Timestamp = DateTime.Now.AddHours(-1)
                               }
                             , new()
                               {
                                       Sender    = Senders.User
                                     , Content   = "Grass is green"
                                     , Timestamp = DateTime.Now.AddHours(-2)
                               }
                             , new()
                               {
                                       Sender    = Senders.User
                                     , Content   = "Roses are red"
                                     , Timestamp = DateTime.Now
                               }
                       };

        var recent = new List<Message> { new() { Content = "sky grass" } };
        var ranked = MemoryService.RankByKeywordOverlap(messages, "sky grass", recent, take: 3)
                                  .ToList();

        foreach (var msg in ranked)
        {
            var score = OverlapScoreWrapper(msg, "sky grass", recent);
            _logger.LogInformation($"Ranked Message: '{msg.Content}' Score: {score} Timestamp: {msg.Timestamp}");
        }
    }

    private void RunBuildSummaryTest()
    {
        _logger.LogInformation("Running BuildSummary test...");

        var stm = new List<Message>
                  {
                          new()
                          {
                                  Sender    = Senders.User
                                , Content   = "User said hello"
                                , Timestamp = DateTime.Now
                          },
                          new()
                          {
                                  Sender    = Senders.Ai
                                , Content   = "AI replied hello"
                                , Timestamp = DateTime.Now
                          }
                  };

        var ltm = new List<Message>
                  {
                          new()
                          {
                                  Sender    = Senders.Ai
                                , Content   = "The sun rises in the east"
                                , Timestamp = DateTime.Now.AddDays(-1)
                          },
                          new()
                          {
                                  Sender    = Senders.User
                                , Content   = "Water is wet"
                                , Timestamp = DateTime.Now.AddDays(-2)
                          }
                  };

        // Example query for scoring
        var query = "sun water hello";

        // --- Score STM messages ---
        var scoredStm = stm.Select(msg =>
        {
            msg.Score = MemoryService.OverlapScore(
                MemoryService.Tokenize(query + " " + string.Join(" ", ltm.Select(m => m.Content ?? ""))),
                MemoryService.Tokenize(msg.Content ?? "")
            );
            return msg;
        }).ToList();

        foreach (var msg in scoredStm)
        {
            _logger.LogInformation($"STM Message: '{msg.Content}' (Score: {msg.Score}) Timestamp: {msg.Timestamp}");
        }

        // --- Score LTM messages ---
        var scoredLtm = MemoryService.RankByKeywordOverlap(ltm, query, stm, take: ltm.Count).ToList();
        foreach (var msg in scoredLtm)
        {
            _logger.LogInformation($"LTM Message: '{msg.Content}' (Score: {msg.Score}) Timestamp: {msg.Timestamp}");
        }

        // --- Build summary using scored messages ---
        string summary = SimpleCompressor.BuildSummary(
            shortTermMemory: scoredStm,
            longTermMemory: scoredLtm,
            maxChars: 1000,
            includeTimestamps: true
        );

        _logger.LogInformation("Summary Output:");
        _logger.LogInformation(summary);
    }

    private void RunBuildSummaryTest_WithImportance()
    {
        _logger.LogInformation("=== RunBuildSummaryTest_WithImportance ===");

        // Arrange: sample messages
        var stmMessages = new List<Message>
                          {
                                  new()
                                  {
                                          Sender     = Senders.User
                                        , Content    = "Casual chit-chat about lunch."
                                        , Importance = 1
                                        , Timestamp  = DateTime.UtcNow.AddSeconds(1)
                                  }
                                , new()
                                  {
                                          Sender     = Senders.Assistant
                                        , Content    = "Responding casually about pizza."
                                        , Importance = 1
                                        , Timestamp  = DateTime.UtcNow.AddSeconds(2)
                                  }
                                , new()
                                  {
                                          Sender     = Senders.User
                                        , Content    = "Critical instruction: remember to log errors."
                                        , Importance = 5
                                        , Timestamp  = DateTime.UtcNow.AddSeconds(3)
                                  }
                                , new()
                                  {
                                          Sender     = Senders.Assistant
                                        , Content    = "Got it. I’ll always log errors clearly."
                                        , Importance = 5
                                        , Timestamp  = DateTime.UtcNow.AddSeconds(4)
                                  }
                                , new()
                                  {
                                          Sender     = Senders.User
                                        , Content    = "Another casual note about weather."
                                        , Importance = 1
                                        , Timestamp  = DateTime.UtcNow.AddSeconds(5)
                                  }
                          };

        var ltmMessages = new List<Message>
                          {
                                  new()
                                  {
                                          Sender     = Senders.Ai
                                        , Content    = "Persisted fact with medium importance"
                                        , Importance = 3
                                        , Timestamp  = DateTime.UtcNow.AddDays(-1)
                                  }
                          };

        // Act: score STM using ApplyScoring (importance-aware)
        MemoryService.ApplyScoring(stmMessages
                                 , "errors logging"
                                 , ltmMessages);

        // Build summary
        var summary = SimpleCompressor.BuildSummary(shortTermMemory: stmMessages
                                                  , longTermMemory: ltmMessages
                                                  , maxChars: 300
                                                  , includeTimestamps: true
        );

        // --- Logging (demo style) ---
        _logger.LogInformation("-- Scored STM Messages --");
        foreach (var msg in stmMessages.OrderByDescending(m => m.Score))
        {
            _logger.LogInformation($"[{msg.Importance}] {msg.Sender}: {msg.Content} (Score={msg.Score:F2})");
        }

        _logger.LogInformation("-- Generated Summary --");
        _logger.LogInformation(summary);

        // --- Assertions (unit-test style, but still logs) ---
        bool promotionOk = summary.Contains("log errors");
        _logger.LogInformation($"ASSERT: Promotion works? {promotionOk}");

        var  topMessage = stmMessages.OrderByDescending(m => m.Score).First();
        bool rankingOk  = topMessage.Importance == 5;
        _logger.LogInformation($"ASSERT: Ranking works? {rankingOk}");

        _logger.LogInformation("=========================================");
    }


    private int OverlapScoreWrapper(Message message, string query, IEnumerable<Message> recent)
    {
        var queryText = query + " " + string.Join(" ", recent.Select(m => m.Content ?? ""));
        var hashSet   = MemoryService.Tokenize(queryText);
        return MemoryService.OverlapScore(hashSet, MemoryService.Tokenize(message.Content ?? ""));
    }
}
#endif
