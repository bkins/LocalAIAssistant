using System.Diagnostics;
using CommunityToolkit.Maui;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;
using LocalAIAssistant.CognitivePlatform.CpClients.Tasks;
using LocalAIAssistant.Core.Environment;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.Knowledge.Journals.ViewModels;
using LocalAIAssistant.Knowledge.Tasks.ViewModels;
using LocalAIAssistant.MarkdownFormatter;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Maui.LifecycleEvents;
using Serilog;
using Serilog.Formatting.Compact;

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
		try
		{
			// Log a message that will definitely be written to the file
			Log.Information("Application startup. Verifying log file write.");

			// Check if the file was created and is not empty
			if (File.Exists(logPath))
			{
				var content = File.ReadAllText(logPath);
				if (!string.IsNullOrEmpty(content))
				{
					Debug.WriteLine($"Log file successfully written. Content length: {content.Length}");
				}
				else
				{
					Debug.WriteLine("Log file exists but is empty.");
				}
			}
			else
			{
				Debug.WriteLine("Log file does not exist after initial log call.");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error writing to log file: {ex.Message}");
		}
		
		// // Log that Serilog is configured
		// Log.Information("Serilog configured. Log file: {LogPath}", logPath);
		// System.Diagnostics.Debug.WriteLine($"Serilog log file path: {logPath}");
  //       
		// // Test Serilog directly
		// Log.Information("This is a test log entry from MauiProgram");
		// Log.Warning("This is a test warning from MauiProgram");
		// Log.Error("This is a test error from MauiProgram");
  //       
		// Check if Serilog wrote to the file
		// if (File.Exists(logPath))
		// {
		// 	var lines = File.ReadAllLines(logPath);
		// 	Debug.WriteLine($"MauiProgram: Serilog file has {lines.Length} lines");
		// 	foreach (var line in lines)
		// 	{
		// 		Debug.WriteLine($"MauiProgram: {line}");
		// 	}
		// }
		// else
		// {
		// 	Debug.WriteLine("MauiProgram: Serilog file does not exist");
		// }

		var builder = MauiApp.CreateBuilder();
		
		builder.Services.AddSingleton<ApiEnvironmentDescriptor>();

		builder.ConfigureFonts(fonts =>
		{
			fonts.AddFont("JetBrainsMono-Regular.ttf"
			            , "JetBrainsMono");
		});

		builder.Services.AddTransient<EnvironmentGuardHandler>();

		builder.Services.AddHttpClient(HttpClientNames.CpApi
		                             , client =>
		                               {
			                               client.BaseAddress = new Uri(BuildEnvironment.ApiBaseUrl);
		                               }).AddHttpMessageHandler<EnvironmentGuardHandler>();
		
		builder.Services.AddHttpClient(HttpClientNames.Ollama
		                             , client =>
		                               {
			                               client.BaseAddress = new Uri(BuildEnvironment.OllamaBaseUrl);
		                               }).AddHttpMessageHandler<EnvironmentGuardHandler>();
		
		builder.Services.AddSingleton<ICognitivePlatformClientFactory, CognitivePlatformClientFactory>();
		builder.Services.AddSingleton<IKnowledgeClientFactory, KnowledgeClientFactory>();
		builder.Services.AddSingleton<IJournalApiClientFactory, JournalApiClientFactory>();
		builder.Services.AddSingleton<ITaskApiClientFactory, TaskApiClientFactory>();

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
		       .ConfigureFonts(fonts =>
		       {
			       fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			       fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
		       })
		       .ConfigureMauiHandlers(handlers => { })
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
		       });

		// Add Serilog to logging
		builder.Logging.AddSerilog();

		// Environment Info and enforcement
		builder.Services.AddSingleton(new ApiEnvironmentDescriptor(BuildEnvironment.Name
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
		
		builder.Configuration.AddJsonFile(ollamaConfigFilePath, optional: false, reloadOnChange: true);

		// Bind Ollama section (you can drop “Ollama” section wrapper if you just want flat file)
		builder.Services.Configure<OllamaConfig>(builder.Configuration.GetSection("Ollama"));

		// Register config service wrapper
		builder.Services.AddSingleton<OllamaConfigService>();

		// IOptionsMonitor lets you subscribe to change notifications:
		//builder.Services.AddSingleton<LlmService>();
		
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
