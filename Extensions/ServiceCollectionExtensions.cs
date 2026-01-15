using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.Knowledge.Journals.Views;
using LocalAIAssistant.Knowledge.Tasks.ViewModels;
using LocalAIAssistant.Knowledge.Tasks.Views;
using LocalAIAssistant.PersonaAndContextEngine;
using LocalAIAssistant.PersonaAndContextEngine.Interfaces;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.ViewModels;
using LocalAIAssistant.Views;
using Microsoft.Extensions.Options;
using Serilog;
using IPersonaRepository = LocalAIAssistant.PersonaAndContextEngine.Interfaces.IPersonaRepository;
using JournalDetailViewModel = LocalAIAssistant.Knowledge.Journals.ViewModels.JournalDetailViewModel;
using KnowledgeInboxViewModel = LocalAIAssistant.Knowledge.Inbox.KnowledgeInboxViewModel;

namespace LocalAIAssistant.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAllServices(this IServiceCollection services
                                                  , string logPath
                                                  , string memoryFilePath)
    {
        services.AddSingleton<ILongTermMemoryStore>(_ => new JsonlAiMemoryStore(memoryFilePath));
        //services.AddSingleton<IConversationMemory, ConversationMemory>();
        services.AddSingleton<IRoleInjectionService, RoleInjectionService>();
        
        services.AddTransient<ILlmService, LlmService>();
        services.AddHttpClient<LlmService>((sp, client) =>
        {
            var config = sp.GetRequiredService<IOptionsMonitor<OllamaConfig>>().CurrentValue;

            client.Timeout     = TimeSpan.FromSeconds(300);
            client.BaseAddress = new Uri(config.Host);
        });
        services.AddSingleton<IPersonalityService, PersonalityService>();
        services.AddSingleton<ApiHealthService>();
        services.AddSingleton<ILogger>(Log.Logger);

        services.AddSingleton<ILoggingService, LoggingService>(s =>
        {
            var logger = s.GetRequiredService<ILogger>();
            return new LoggingService(logger, logPath);
        });
        // services.AddSingleton<ILoggingService, LoggingService>(s =>
        // {
        //     var loggerService = s.GetRequiredService<ILogger<LoggingService>>();
        //     return new LoggingService(loggerService, logPath);
        // });
        
        services.Configure<MemoryRetrievalOptions>(opts =>
        {
            opts.MaxStmMessages    = 6;
            opts.MaxLtmSnippets    = 6;
            opts.SummaryMaxChars   = 1200;
            opts.LtmRecencyWindow  = TimeSpan.FromDays(90);
            opts.IncludeTimestamps = false;
        });
        
        services.AddSingleton<OllamaConfigService>();
        services.AddSingleton<IOrchestratorService, OrchestratorService>();
        services.AddSingleton<IPersonaAndContextEngine, PersonaAndContextEngine.PersonaAndContextEngine>();
        services.AddSingleton<IPersonaRepository, InMemoryPersonaRepository>();
        
        services.AddSingleton<IIntentAnalyzer, RuleBasedIntentAnalyzer>();
        services.Decorate<IIntentAnalyzer, HybridIntentAnalyzer>();
        /*
         * The `Decorate` method is part of the Scrutor NuGet. The above two line is equivalent to:
        ```
        services.AddSingleton<RuleBasedIntentAnalyzer>();
        services.AddSingleton<IIntentAnalyzer>
        (
            sp => new HybridIntentAnalyzer(sp.GetRequiredService<ILlmService>()
                                         , sp.GetRequiredService<RuleBasedIntentAnalyzer>())
        ); 
        ```        
         */

        return services;
    }
    
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<LogsViewModel>();
        services.AddTransient<MemoryManagementViewModel>();
        services.AddTransient<ApiHealthViewModel>();
        services.AddSingleton<SettingsViewModel>();
        
        services.AddSingleton<ChatViewModel>();
        services.AddTransient<MainViewModel>();
        
        services.AddTransient<AppShellViewModel>();
        services.AddSingleton<AppShellMasterViewModel>();
        
        // Knowledge and Knowledge Clients
        services.AddTransient<KnowledgeInboxViewModel>();
        services.AddTransient<JournalDetailViewModel>();
        services.AddTransient<TaskDetailViewModel>();
        
        return services;
    }
    
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<MainPage>();
        services.AddTransient<LogsPage>();
        services.AddTransient<MemoryManagementPage>();
        services.AddTransient<AppShell>();
        services.AddSingleton<SettingsPage>();
        
        // Knowledge and Knowledge Clients
        services.AddTransient<KnowledgeInboxPage>();
        services.AddTransient<JournalDetailPage>();
        services.AddTransient<TaskDetailPage>();
        
        return services;
    }
}