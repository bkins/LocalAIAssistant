using System.Diagnostics;
using CommunityToolkit.Maui;
using CP.Client.Core.Common.ConnectivityToApi;
using CP.Client.Core.Web;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;
using LocalAIAssistant.CognitivePlatform.CpClients.Notifications;
using LocalAIAssistant.Core.Notifications;
using LocalAIAssistant.CognitivePlatform.CpClients.BrainDump;
using LocalAIAssistant.CognitivePlatform.CpClients.Coco;
using LocalAIAssistant.CognitivePlatform.CpClients.Tasks;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using LocalAIAssistant.Core.Environment;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.Knowledge.Journals.ViewModels;
using LocalAIAssistant.Knowledge.Tasks.ViewModels;
using LocalAIAssistant.MarkdownFormatter;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.FileSync;
using LocalAIAssistant.Services.Health;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Maui.LifecycleEvents;
using Serilog;
using Serilog.Formatting.Compact;
#if ANDROID
using LocalAIAssistant.Platforms.Android.Health;
using LocalAIAssistant.Platforms.Android.Handlers;
#endif

namespace LocalAIAssistant;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var logPath		   = Path.Combine(FileSystem.AppDataDirectory, "logs", "app-log.jsonl");
		//var memoryFilePath = Path.Combine(FileSystem.AppDataDirectory, "ai_memory.jsonl");
		
		Log.Logger = new LoggerConfiguration().MinimumLevel.Information() // Log information, warnings, and errors
		                                      .WriteTo.File(new CompactJsonFormatter()
		                                                  , logPath
		                                                  , rollingInterval: RollingInterval.Infinite
		                                                  , retainedFileCountLimit: null
		                                                  , buffered: false
		                                                  , shared: true)
		                                      .WriteTo.Debug()
		                                      .CreateLogger();
		Debug.WriteLine($"Configuring Serilog to log to: {logPath}");
		
		var builder = MauiApp.CreateBuilder();

		builder.ConfigureFonts(fonts =>
		{
			fonts.AddFont("JetBrainsMono-Regular.ttf"
			            , "JetBrainsMono");
		});

		builder.Services.AddTransient<EnvironmentGuardHandler>();

		builder.Services
		       .AddHttpClient(HttpClientNames.CpApi
		                    , client =>
		                      {
			                      client.BaseAddress = new Uri(BuildEnvironment.ApiBaseUrl);
		                      })
		       .AddHttpMessageHandler<EnvironmentGuardHandler>();

		builder.Services
		       .AddHttpClient(HttpClientNames.Ollama
		                    , client =>
		                      {
			                      client.BaseAddress = new Uri(BuildEnvironment.OllamaBaseUrl);
		                      })
		       .AddHttpMessageHandler<EnvironmentGuardHandler>();
		
		builder.Services.AddSingleton<ICognitivePlatformClientFactory, CognitivePlatformClientFactory>();
		builder.Services.AddSingleton<IKnowledgeClientFactory, KnowledgeClientFactory>();
		builder.Services.AddSingleton<IJournalApiClientFactory, JournalApiClientFactory>();
		builder.Services.AddSingleton<ITaskApiClientFactory, TaskApiClientFactory>();
		builder.Services.AddSingleton<INotificationApiClientFactory, NotificationApiClientFactory>();
		builder.Services.AddSingleton<IBrainDumpApiClientFactory, BrainDumpApiClientFactory>();
		builder.Services.AddSingleton<ICocoApiClientFactory, CocoApiClientFactory>();
		// Guard rules (MaxPerDay, MinGapMinutes, QuietHoursStart/End) are server-side.
		// Change them in CognitivePlatform/appsettings.json under "Notifications".
		builder.Services.AddSingleton<INotificationScheduler, PluginLocalNotificationScheduler>();
		builder.Services.AddSingleton<NotificationSyncService>();

		//Markdown formatters:
		builder.Services.AddSingleton<IMarkdownFormatter<TaskDetailViewModel>, TaskMarkdownFormatter>();
		builder.Services.AddSingleton<IMarkdownFormatter<JournalDetailViewModel>, JournalMarkdownFormatter>();
		
		// Bind Ollama config (with validation)
		builder.Services
		       .AddOptions<OllamaConfig>()
		       .Bind(builder.Configuration.GetSection("Ollama"))
		       .ValidateDataAnnotations();

		builder.UseMauiApp<App>()
		       .UseMauiCommunityToolkit()
		       .UseLocalNotification(config =>
		       {
#if ANDROID
			       config.AddAndroid(android =>
			       {
				       android.AddChannel(new NotificationChannelRequest
				              {
				                  Id         = "cp-reminders"
				                , Name       = "CP Reminders"
				                , Importance = AndroidImportance.Default
				              });
			       });
#endif
		       })
		       .ConfigureFonts(fonts =>
		       {
			       fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			       fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
		       })
		       .ConfigureMauiHandlers(handlers =>
		       {
#if ANDROID
			       handlers.AddHandler(typeof(Editor), typeof(MinHeightEditorHandler));
#endif
		       })
		       .ConfigureLifecycleEvents(lifecycle =>
		       {
#if ANDROID
			       lifecycle.AddAndroid(android =>
			       {
				       android.OnCreate((activity, bundle) =>
				       {
					       Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
					       {
						       Debug.WriteLine($"[ANDROID CRASH] {args.Exception}");
						       args.Handled = true; // Prevents hard crash — remove once you've identified issues
					       };
				       });
			       });
#endif
#if WINDOWS
			       lifecycle.AddWindows(windows =>
			       {
				       windows.OnClosed((window, _) =>
				       {
					       var hotkeyService = IPlatformApplication.Current?.Services
					                                                         .GetService<IGlobalHotkeyService>();
					       hotkeyService?.Unregister();

					       var clipboardMonitor = IPlatformApplication.Current?.Services
					                                                            .GetService<IClipboardMonitorService>();
					       clipboardMonitor?.Stop();
				       });
			       });
#endif
		       });

		// Add Serilog to logging
		builder.Logging.AddSerilog();

		// Environment Info and enforcement
		builder.Services.AddSingleton(new Services.ApiEnvironmentDescriptor(BuildEnvironment.Name
		                                                                                  , BuildEnvironment.ApiBaseUrl
		                                                                                  , BuildEnvironment.OllamaBaseUrl));
		builder.Services.AddSingleton(new CP.Client.Core.Web.ApiEnvironmentDescriptor(BuildEnvironment.Name
		                                                                             , BuildEnvironment.ApiBaseUrl
		                                                                             , BuildEnvironment.OllamaBaseUrl));
		
		builder.Services.AddSingleton<EnvironmentHandshakeState>();
		builder.Services.AddSingleton<StartupHandshakeService>();
		builder.Services.AddSingleton<WriteGuard>();



		// Paths
		var appDir = FileSystem.AppDataDirectory;
		
		var memoryFilePath 		 = Path.Combine(appDir, "ai_memory.jsonl"); // JSONL for messages
		var factsFilePath  		 = Path.Combine(appDir, "facts.json");      // JSON for k/v facts
		var ollamaConfigFilePath = Path.Combine(appDir, "OllamaConfig.json");
		
		if (File.Exists(ollamaConfigFilePath).Not())
		{
			File.WriteAllText(ollamaConfigFilePath, "{}");
		}
		
		builder.Services
		       .AddOptions<OllamaConfig>()
		       .BindConfiguration("")
		       .ValidateDataAnnotations();
		
		builder.Services.AddSingleton<IOptionsChangeTokenSource<OllamaConfig>>(new FileOptionsSource<OllamaConfig>(ollamaConfigFilePath));
		
		// Your SQLite connection string for STM
		//var sqliteConnStr  = "TODO: your SQLite connection string";
		builder.Services.AddDbContext<LocalAiAssistantDbContext>(options =>
		{
			var dbPath = Path.Combine(FileSystem.AppDataDirectory, "localaiassistant.db");
			options.UseSqlite($"Filename={dbPath}");
		});

		builder.Services.AddScoped<DatabaseInitializer>();
		
		var localDbPath = Path.Combine(FileSystem.AppDataDirectory, "knowledge_local.db");

		builder.Services.AddSingleton<ILocalKnowledgeStore>(_ => new SqliteLocalKnowledgeStore(localDbPath));
		builder.Services.AddSingleton<IKnowledgeSyncService, KnowledgeSyncService>();
		builder.Services.AddTransient<KnowledgeInboxViewModel>();
		builder.Services.AddTransient<KnowledgeInboxPage>();
		
		builder.Services.AddSingleton<IOfflineQueueService, OfflineQueueService>();
		builder.Services.AddSingleton<QueueReplayCoordinator>();
		
		builder.Services.AddSingleton<UsageService>();
		builder.Services.AddSingleton<UsageViewModel>();
		
		builder.Configuration.AddJsonFile(ollamaConfigFilePath, optional: false, reloadOnChange: true);

		// Bind Ollama section (you can drop “Ollama” section wrapper if you just want flat file)
		builder.Services.Configure<OllamaConfig>(builder.Configuration.GetSection("Ollama"));

		// Register config service wrapper
		builder.Services.AddSingleton<OllamaConfigService>();

		// IOptionsMonitor lets you subscribe to change notifications:
		//builder.Services.AddSingleton<LlmService>();
		
		// Health Connect gateway
		builder.Services
		       .AddOptions<HealthGatewayConfig>()
		       .Bind(builder.Configuration.GetSection("HealthGateway"));
#if ANDROID
		builder.Services.AddSingleton<IHealthConnectManager, HealthConnectManager>();
#endif
		builder.Services.AddHostedService<HealthApiService>();

		// Load bundled appsettings.json (FileGateway defaults and other sections)
		try
		{
			using var bundledStream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
			var       memoryStream  = new MemoryStream();
			bundledStream.CopyTo(memoryStream);
			memoryStream.Position = 0;
			builder.Configuration.AddJsonStream(memoryStream);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[MauiProgram] Could not load bundled appsettings.json: {ex.Message}");
		}

		builder.Services
		       .AddOptions<FileGatewayConfig>()
		       .Bind(builder.Configuration.GetSection("FileGateway"));

		builder.Services.AddHostedService<FileGatewayService>();

		builder.Services.AddAllServices(logPath, memoryFilePath);
		builder.Services.AddAiMemoryServices(Path.Combine(FileSystem.AppDataDirectory, "Memory.db")
		                                   , memoryFilePath
		                                   , factsFilePath
		                                   , memoryFilePath);
		
		builder.Services.AddViewModels();
		builder.Services.AddViews();
		
		// CP.Client.Core:
		builder.Services.AddSingleton<ConnectivityState>();
		builder.Services.AddSingleton<IConnectivityState>(sp => sp.GetRequiredService<ConnectivityState>());
		builder.Services.AddSingleton<IConnectivityReporter>(sp => sp.GetRequiredService<ConnectivityState>());

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
	
		using var scope = app.Services.CreateScope();

		var db          = scope.ServiceProvider.GetRequiredService<LocalAiAssistantDbContext>();
		var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
		
		// Blocking is ok in this case
		initializer.InitializeAsync().GetAwaiter().GetResult();
		
		var coordinator = scope.ServiceProvider
		                       .GetRequiredService<QueueReplayCoordinator>();
		return app;
	}
}
