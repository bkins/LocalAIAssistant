using LocalAIAssistant.Core.Environment;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Health;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using LocalAIAssistant.ViewModels;
using LocalAIAssistant.Services.FileSync;
using LocalAIAssistant.Data;

namespace LocalAIAssistant;

public partial class App : Application
{
	private readonly ApiHealthService                _apiHealthService;
	private readonly ILoggingService                 _loggingService;
	private readonly AppShellMasterViewModel         _masterViewModel;
	private readonly NotificationSyncService         _notificationSyncService;
	private readonly HealthPushService               _healthPushService;
	private readonly FileGatewayService              _fileGatewayService;
	private readonly CancellationTokenSource         _appLifetime = new();

	public App (ApiHealthService          apiHealthService
	          , ILoggingService           loggingService
	          , AppShellMasterViewModel   masterViewModel
	          , NotificationSyncService   notificationSyncService
	          , HealthPushService         healthPushService
	          , FileGatewayService        fileGatewayService)
	{
		InitializeComponent();

		_apiHealthService        = apiHealthService;
		_loggingService          = loggingService;
		_masterViewModel         = masterViewModel;
		_notificationSyncService = notificationSyncService;
		_healthPushService       = healthPushService;
		_fileGatewayService      = fileGatewayService;

		RegisterGlobalExceptionHandlers();
	}


	
	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell(_masterViewModel));
	}
	protected override async void OnStart()
	{
		try
		{
			var isProd                    = BuildEnvironment.Name.EqualsIgnoreCase("PROD");
			var defaultDiagnosticsEnabled = !isProd;

			var apiHealthService = Handler?.MauiContext?.Services.GetRequiredService<ApiHealthService>();
			if (apiHealthService != null && Preferences.Default.Get(StringConsts.EnableStartupProbesPrefKey, true)) 
			{
				await apiHealthService.InitializeAsync().ConfigureAwait(false);
			}
        
			var handshake = Handler?.MauiContext?.Services.GetRequiredService<StartupHandshakeService>();
			if (handshake != null && Preferences.Default.Get(StringConsts.EnableStartupDiagnosticsPrefKey, defaultDiagnosticsEnabled))
			{
				await handshake.RunAsync(BuildEnvironment.Name);
			}


			// Start the health data push loop. MAUI does not automatically start
			// IHostedService instances — we must call StartAsync manually.
			await _healthPushService.StartAsync(_appLifetime.Token).ConfigureAwait(false);

			// Start the file sync gateway listener.
			await _fileGatewayService.StartAsync(_appLifetime.Token).ConfigureAwait(false);

			// Synchronize proactive notifications with the OS notification scheduler.
			await _notificationSyncService.SyncAsync().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnStart failed: {ex}");
			_loggingService.LogError(ex, "OnStart failed", Category.App);
		}
	}
	
	protected override async void OnResume()
	{
		try
		{
			await _notificationSyncService.SyncAsync();
		}
		catch (Exception ex)
		{
			_loggingService.LogError(ex, "OnResume notification sync failed", Category.App);
		}
	}

	private void RegisterGlobalExceptionHandlers()
	{
		// .NET unhandled exceptions (non-UI thread)
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			if (args.ExceptionObject is Exception ex)
			{
				LogCrash("AppDomain.UnhandledException", ex);
			}
			else
			{
				LogCrash("AppDomain.UnhandledException", new Exception($"Non-exception object thrown: {args.ExceptionObject}"));
			}
		};

		// Async void / Task exceptions that weren't awaited
		TaskScheduler.UnobservedTaskException += (sender, args) =>
		{
			if (args.Exception != null)
			{
				LogCrash("TaskScheduler.UnobservedTaskException", args.Exception);
				args.SetObserved(); // Prevents process termination
			}
		};
	}

	private void LogCrash(string source, Exception? ex)
	{
		var message = $"[CRASH] Source: {source}\n{ex}";
		System.Diagnostics.Debug.WriteLine(message);
		_loggingService?.LogError(ex!, message, Category.App);
	}
	
}
