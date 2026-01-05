using System.Diagnostics;
using LocalAIAssistant.CognitivePlatform;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Knowledge.Clients;
using LocalAIAssistant.Knowledge.Journals.Clients;
using LocalAIAssistant.Knowledge.Tasks.Clients;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
		if (File.Exists(logPath))
		{
			var lines = File.ReadAllLines(logPath);
			Debug.WriteLine($"MauiProgram: Serilog file has {lines.Length} lines");
			foreach (var line in lines)
			{
				Debug.WriteLine($"MauiProgram: {line}");
			}
		}
		else
		{
			Debug.WriteLine("MauiProgram: Serilog file does not exist");
		}

		var builder = MauiApp.CreateBuilder();
		
		builder.ConfigureFonts(fonts =>
		{
			fonts.AddFont("JetBrainsMono-Regular.ttf"
			            , "JetBrainsMono");
		});

		var cognitivePlatformApi = new HttpClient
		                            {
				                            BaseAddress = new Uri("http://192.168.0.33:5272")
				                          , Timeout     = TimeSpan.FromSeconds(500)
		                            };
		
		builder.Services.AddHttpClient<ICognitivePlatformClient, CognitivePlatformClient>(client =>
		{
			// Should this be the address to the CP API? - http://localhost:5272
			client.BaseAddress = cognitivePlatformApi.BaseAddress; // Physical Device //"http://10.0.2.2:5272/");  //"http://10.0.2.2:5200/"); // Android emulator
			client.Timeout     = cognitivePlatformApi.Timeout;     
		});
		
		builder.Services.AddHttpClient<IKnowledgeApiClient, KnowledgeApiClient>(client =>
		{
			client.BaseAddress = cognitivePlatformApi.BaseAddress; 
			client.Timeout     = cognitivePlatformApi.Timeout;     
		});

		builder.Services.AddHttpClient<IJournalApiClient, JournalApiClient>(client =>
		{
			client.BaseAddress = cognitivePlatformApi.BaseAddress; 
			client.Timeout     = cognitivePlatformApi.Timeout;     
		});
		
		builder.Services.AddHttpClient<ITaskApiClient, TaskApiClient>(client =>
		{
			client.BaseAddress = cognitivePlatformApi.BaseAddress; 
			client.Timeout     = cognitivePlatformApi.Timeout;     
		});
		
		// Bind Ollama config (with validation)
		builder.Services
		       .AddOptions<OllamaConfig>()
		       .Bind(builder.Configuration.GetSection("Ollama"))
		       .ValidateDataAnnotations();

		builder.UseMauiApp<App>()
		       .ConfigureFonts(fonts =>
		       {
			       fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			       fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
		       });

		// Add Serilog to logging
		builder.Logging.AddSerilog();
		
		
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
		
		builder.Services.AddSingleton<IOptionsChangeTokenSource<OllamaConfig>>(
			new FileOptionsSource<OllamaConfig>(ollamaConfigFilePath));
		
		// Your SQLite connection string for STM
		var sqliteConnStr  = "TODO: your SQLite connection string";

		builder.Configuration.AddJsonFile(ollamaConfigFilePath, optional: false, reloadOnChange: true);

		// Bind Ollama section (you can drop “Ollama” section wrapper if you just want flat file)
		builder.Services.Configure<OllamaConfig>(builder.Configuration.GetSection("Ollama"));

		// Register config service wrapper
		builder.Services.AddSingleton<OllamaConfigService>();

		// IOptionsMonitor lets you subscribe to change notifications:
		//builder.Services.AddSingleton<LlmService>();
		
		builder.Services.AddAllServices(logPath, memoryFilePath);
		builder.Services.AddAiMemoryServices(sqliteConnStr
		                                   , memoryFilePath
		                                   , factsFilePath
		                                   , memoryFilePath);
		
		builder.Services.AddViewModels();
		builder.Services.AddViews();
		
#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Ensure STM is loaded into session on startup (synchronous ok for now)
		var cm = app.Services.GetRequiredService<IConversationMemory>();
		cm.InitializeAsync().GetAwaiter().GetResult();

		return app;
	}
}
