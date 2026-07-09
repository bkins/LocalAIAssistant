using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LocalAIAssistant.Extensions;

namespace LocalAIAssistant.ViewModels
{
    public partial class AppShellViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _hasErrorsInLogs=true;
        [ObservableProperty]
        private bool _hasNoErrorsInLogs;
        
        public AppShellViewModel()
        {
            WeakReferenceMessenger.Default.Register<LogErrorsChangedMessage>(this, (recipient, message) =>
            {
                ((AppShellViewModel)recipient).HasErrorsInLogs = message.HasErrors;
                ((AppShellViewModel)recipient).HasNoErrorsInLogs = message.HasErrors.Not();
            });
        }
    }
}