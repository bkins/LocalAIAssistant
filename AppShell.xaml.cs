using LocalAIAssistant.Services;
using LocalAIAssistant.ViewModels;
using LocalAIAssistant.Views;

namespace LocalAIAssistant;

public partial class AppShell : Shell
{

	private readonly ApiHealthService _apiHealthService;

	public AppShell(ApiHealthService   apiHealthService
	              , ApiHealthViewModel apiHealthViewModel)
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(LogDetailPage)
		                    , typeof(LogDetailPage));

		_apiHealthService = apiHealthService;

		BindingContext = apiHealthViewModel;

		_ = apiHealthViewModel.CheckApiStatusAsync();
	}

}
