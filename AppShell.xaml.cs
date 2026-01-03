using LocalAIAssistant.Services;
using LocalAIAssistant.ViewModels;
using LocalAIAssistant.Views;

namespace LocalAIAssistant;

public partial class AppShell : Shell
{

	private readonly ApiHealthService _apiHealthService;

	public AppShell(AppShellMasterViewModel masterViewModel)
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(LogDetailPage)
		                    , typeof(LogDetailPage));

		BindingContext = masterViewModel;

		// Trigger the initial API status check via the master view model's property
		_ = masterViewModel.ApiHealthViewModel.CheckApiStatusAsync();
	}

}
