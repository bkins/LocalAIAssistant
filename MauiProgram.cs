using LocalAIAssistant.Extensions;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using Microsoft.Extensions.Logging;
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
			System.Diagnostics.Debug.WriteLine($"MauiProgram: Serilog file has {lines.Length} lines");
			foreach (var line in lines)
			{
				System.Diagnostics.Debug.WriteLine($"MauiProgram: {line}");
			}
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("MauiProgram: Serilog file does not exist");
		}

		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>()
		       .ConfigureFonts(fonts =>
		       {
			       fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			       fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
		       });

		// Add Serilog to logging
		builder.Logging.AddSerilog();
		
		
		// Paths
		var appDir         = FileSystem.AppDataDirectory;
		var memoryFilePath = Path.Combine(appDir, "ai_memory.jsonl"); // JSONL for messages
		var factsFilePath  = Path.Combine(appDir, "facts.json");      // JSON for k/v facts

		// Your SQLite connection string for STM
		var sqliteConnStr  = "TODO: your SQLite connection string";

		builder.Services.AddAiMemoryServices(sqliteConnStr
		                                   , memoryFilePath
		                                   , factsFilePath
		                                   , memoryFilePath);
		
		builder.Services.AddAllServices(logPath, memoryFilePath);
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
