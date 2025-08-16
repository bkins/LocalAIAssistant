using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Logging;

namespace LocalAIAssistant.Extensions;

public static class AiMemoryServiceCollectionExtensions
{

    public static IServiceCollection AddAiMemoryServices(this IServiceCollection services
                                                       , string                  dbPath                 // For STM messages
                                                       , string                  jsonlMessagesPath      // For LTM messages (JSONL)
                                                       , string                  factsJsonPath          // MemoryService k/v facts (JSON)
                                                       , string                  memoryFilePath)
    {
        // Marker interfaces: IShortTermMemoryStore / ILongTermMemoryStore both inherit IAiMemoryStore
        services.AddSingleton<IShortTermMemoryStore>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggingService>();
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "AiMemory.db");
            return new SqliteAiMemoryStore(logger, dbPath);
        });

        services.AddSingleton<ILongTermMemoryStore>(_ => new JsonlAiMemoryStore(jsonlMessagesPath));
        

        services.AddSingleton<IConversationMemory>(provider => new ConversationMemory(provider.GetRequiredService<IShortTermMemoryStore>()
                                                                                    , provider.GetRequiredService<ILongTermMemoryStore>()));

        services.AddSingleton<IMemoryService>(sp => new MemoryService(sp.GetRequiredService<IConversationMemory>()
                                                                    , sp.GetServices<IAiMemoryStore>()
                                                                    , factsJsonPath
                                                                    , memoryFilePath));

        return services;
    }

}
