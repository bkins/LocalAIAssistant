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
            var fullDbPath = Path.Combine(FileSystem.AppDataDirectory, dbPath);
            return new SqliteAiMemoryStore(logger, fullDbPath);
        });
        services.AddSingleton<IAiMemoryStore>(sp => sp.GetRequiredService<IShortTermMemoryStore>());
        services.AddSingleton<ILongTermMemoryStore>(_ => new JsonlAiMemoryStore(jsonlMessagesPath));
        services.AddSingleton<IAiMemoryStore>(sp => sp.GetRequiredService<ILongTermMemoryStore>());
        
        services.AddSingleton<IConversationMemory, ConversationMemory>();

        // services.AddSingleton<IConversationMemory>(provider => new ConversationMemory(provider.GetRequiredService<IShortTermMemoryStore>()
        //                                                                             , provider.GetRequiredService<ILongTermMemoryStore>()
        //                                                                               , provider.GetRequiredService<MemoryRetrievalOptions>()));

        services.AddSingleton<IMemoryService>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggingService>();
            return new MemoryService(sp.GetRequiredService<IConversationMemory>()
                                   , sp.GetServices<IAiMemoryStore>()
                                   , factsJsonPath
                                     , logger);
        });

        return services;
    }

}
