using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant;

public partial class App : Application
{
	private readonly ILlmService      _ollamaApiService;
	private readonly ApiHealthService _apiHealthService;
	private readonly ApiHealthViewModel _apiHealthViewModel;
	
	public App(ILlmService ollamaApiService,  ApiHealthService apiHealthService,  ApiHealthViewModel apiHealthViewModel)
	{
		InitializeComponent();
		
		_ollamaApiService = ollamaApiService;
		_apiHealthService = apiHealthService;
		_apiHealthViewModel = apiHealthViewModel;
		
		//CheckApiAvailabilityAsync(_ollamaApiService);
	}
	private async void CheckApiAvailabilityAsync(ILlmService ollamaApiService)
	{
		var isAvailable = await ollamaApiService.CheckApiHealthAsync();
		_apiHealthService.IsApiAvailable = isAvailable;
	}
	
	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell(_apiHealthService, _apiHealthViewModel));
	}
	protected override async void OnStart()
	{
		var apiHealthService = Handler?.MauiContext?.Services.GetRequiredService<ApiHealthService>();
		if (apiHealthService != null) await apiHealthService.InitializeAsync().ConfigureAwait(false);
	}
}