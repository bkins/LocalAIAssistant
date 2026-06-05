using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.Knowledge.Journals.Views;
using LocalAIAssistant.Knowledge.Tasks.Views;
using LocalAIAssistant.Services;
using LocalAIAssistant.ViewModels;
using LocalAIAssistant.Views;

namespace LocalAIAssistant;

public partial class AppShell : Shell
{
	private readonly AppShellMasterViewModel _viewModel;
#if ANDROID
	private bool _debugPageShown;
#endif

	public AppShell(AppShellMasterViewModel masterViewModel)
	{
		BindingContext = masterViewModel;
		_viewModel     = masterViewModel;

		InitializeComponent();

//Main / Chat
		Routing.RegisterRoute(nameof(MainPage)
		                    , typeof(MainPage));
// Settings
		Routing.RegisterRoute(nameof(SettingsPage)
		                    , typeof(SettingsPage));
// Logs
		Routing.RegisterRoute(nameof(LogDetailPage)
		                    , typeof(LogDetailPage));

// Knowledge Inbox
		Routing.RegisterRoute(nameof(KnowledgeInboxPage)
		                    , typeof(KnowledgeInboxPage));
// Journals
		Routing.RegisterRoute(nameof(JournalDetailPage)
		                    , typeof(JournalDetailPage));

		Routing.RegisterRoute(nameof(JournalRevisionHistoryPage)
		                    , typeof(JournalRevisionHistoryPage));

		Routing.RegisterRoute(nameof(EditJournalEntryPage)
		                    , typeof(EditJournalEntryPage));

// Tasks
		Routing.RegisterRoute(nameof(TaskDetailPage)
		                    , typeof(TaskDetailPage));

		Routing.RegisterRoute(nameof(EditTaskPage)
		                    , typeof(EditTaskPage));
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.InitializeAsync();

#if ANDROID
		// Show startup diagnostics as a modal so AppShell is always the window root.
		// Avoids the ContentPage→Shell platform transition that breaks Android touch dispatch.
		if (!_debugPageShown)
		{
			_debugPageShown = true;
			await Navigation.PushModalAsync(new Views.DebugStartupPage(), animated: false);
		}
#endif
	}

}
