using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
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
		
		// envService.InitializeAsync(ApiEnvironment.Qa).Wait();
		
		AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;

		//CheckApiAvailabilityAsync(_ollamaApiService);
	}

	private void OnCurrentDomainOnUnhandledException(object                      sender
	                                               , UnhandledExceptionEventArgs eventArgs)
	{
		var exception = eventArgs.ExceptionObject as Exception;
		if (exception != null)
		{
			var message = $"Unhandled exception: {exception}";
			// Log the exception details here.
			// For example, you can write to the console or a file.
			System.Diagnostics.Debug.WriteLine(message);
			
			_loggingService.LogError(exception
			                       , $"Global Unhandled Exception occurred: {exception.Message}"
			                       , Category.App);
		}
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
		var apiHealthService = Handler?.MauiContext?.Services.GetRequiredService<ApiHealthService>();
		if (apiHealthService != null) await apiHealthService.InitializeAsync().ConfigureAwait(false);
		
	}
}