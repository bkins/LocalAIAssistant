using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.Knowledge.Journals.Views;
using LocalAIAssistant.Knowledge.Tasks.Views;
using LocalAIAssistant.Services;
using LocalAIAssistant.ViewModels;
using LocalAIAssistant.Views;

namespace LocalAIAssistant;

public partial class AppShell : Shell
{
	private readonly ApiHealthService        _apiHealthService;
	private readonly AppShellMasterViewModel _viewModel;
	
	public AppShell(AppShellMasterViewModel masterViewModel)
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(LogDetailPage)
		                    , typeof(LogDetailPage));
		
		Routing.RegisterRoute(nameof(KnowledgeInboxPage)
		                    , typeof(KnowledgeInboxPage));

		Routing.RegisterRoute(nameof(JournalDetailPage)
		                    , typeof(JournalDetailPage));
		
		Routing.RegisterRoute(nameof(TaskDetailPage)
		                    , typeof(TaskDetailPage));

		BindingContext = masterViewModel;
		_viewModel     = masterViewModel;
		
		// Trigger the initial API status check via the master view model's property
		_ = _viewModel.ApiHealthViewModel.CheckApiStatusAsync();
	}
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.InitializeAsync();
	}

}
