using LocalAIAssistant.Services;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.ViewModels;
using LocalAIAssistant.Views;
using Microsoft.Extensions.Logging;

namespace LocalAIAssistant.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAllServices(this IServiceCollection services, string logPath, string memoryFilePath)
    {
        services.AddSingleton<ILongTermMemoryStore>(_ => new JsonlAiMemoryStore(memoryFilePath));
        services.AddSingleton<IConversationMemory, ConversationMemory>();
        services.AddSingleton<IRoleInjectionService, RoleInjectionService>();
        
        services.AddTransient<ILlmService, LlmService>();
        services.AddSingleton<IPersonalityService, PersonalityService>();
        services.AddSingleton<ApiHealthService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<ILoggingService, LoggingService>(s =>
        {
            var loggerService = s.GetRequiredService<ILogger<LoggingService>>();
            return new LoggingService(loggerService, logPath);
        });
        services.Configure<MemoryRetrievalOptions>(opts =>
        {
            opts.MaxStmMessages    = 6;
            opts.MaxLtmSnippets    = 6;
            opts.SummaryMaxChars   = 1200;
            opts.LtmRecencyWindow  = TimeSpan.FromDays(90);
            opts.IncludeTimestamps = false;
        });
        return services;
    }
    
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<LogsViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MemoryManagementViewModel>();
        services.AddTransient<ApiHealthViewModel>();
        
        return services;
    }
    
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<MainPage>();
        services.AddTransient<LogsPage>();
        services.AddTransient<MemoryManagementPage>();
        
        return services;
    }
}