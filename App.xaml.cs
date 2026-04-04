using LocalAIAssistant.Core.Environment;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant;

public partial class App : Application
{
	private readonly ILlmService                     _ollamaApiService;
	private readonly ApiHealthService                _apiHealthService;
	private readonly ApiHealthViewModel              _apiHealthViewModel;
	private readonly ILoggingService                 _loggingService;
	private readonly AppShellMasterViewModel         _masterViewModel;

	public App (ILlmService             ollamaApiService
	          , ApiHealthService        apiHealthService
	          , ApiHealthViewModel      apiHealthViewModel
	          , ILoggingService         loggingService
	          , AppShellMasterViewModel masterViewModel)
	{
		InitializeComponent();

		_ollamaApiService   = ollamaApiService;
		_apiHealthService   = apiHealthService;
		_apiHealthViewModel = apiHealthViewModel;
		_loggingService     = loggingService;
		_masterViewModel    = masterViewModel;
		
		RegisterGlobalExceptionHandlers();
		
		AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
		
	}

	private void OnCurrentDomainOnUnhandledException(object                      sender
	                                               , UnhandledExceptionEventArgs eventArgs)
	{
		var exception = eventArgs.ExceptionObject as Exception;
		
		if (exception == null) return;
		
		var message = $"Unhandled exception: {exception}";
		
		System.Diagnostics.Debug.WriteLine(message);
			
		_loggingService.LogError(exception
		                       , $"Global Unhandled Exception occurred: {exception.Message}"
		                       , Category.App);
	}

	private async void CheckApiAvailabilityAsync(ILlmService ollamaApiService)
	{
		var isAvailable = await ollamaApiService.CheckApiHealthAsync();
		_apiHealthService.IsApiAvailable = isAvailable;
	}
	
	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell(_masterViewModel));
	}
	protected override async void OnStart()
	{
		try
		{
			var apiHealthService = Handler?.MauiContext?.Services.GetRequiredService<ApiHealthService>();
			if (apiHealthService != null) 
				await apiHealthService.InitializeAsync().ConfigureAwait(false);
        
			var handshake = Handler?.MauiContext?.Services.GetRequiredService<StartupHandshakeService>();
			if (handshake != null)
				await handshake.RunAsync(BuildEnvironment.Name);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnStart failed: {ex}");
			_loggingService.LogError(ex, "OnStart failed", Category.App);
		}
	}
	
	private void RegisterGlobalExceptionHandlers()
	{
		// .NET unhandled exceptions (non-UI thread)
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			var ex = args.ExceptionObject as Exception;
			LogCrash("AppDomain.UnhandledException", ex);
		};

		// Async void / Task exceptions that weren't awaited
		TaskScheduler.UnobservedTaskException += (sender, args) =>
		{
			LogCrash("TaskScheduler.UnobservedTaskException", args.Exception);
			args.SetObserved(); // Prevents process termination
		};
	}

	private void LogCrash(string source, Exception? ex)
	{
		var message = $"[CRASH] Source: {source}\n{ex}";
		System.Diagnostics.Debug.WriteLine(message);
		_loggingService?.LogError(ex!, message, Category.App);
	}
	
}
