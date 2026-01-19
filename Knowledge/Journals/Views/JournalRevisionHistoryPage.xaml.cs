using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalAIAssistant.Knowledge.Journals.ViewModels;

namespace LocalAIAssistant.Knowledge.Journals.Views;

public partial class JournalRevisionHistoryPage : ContentPage
{
    private readonly JournalRevisionHistoryViewModel _historyViewModel;
    public JournalRevisionHistoryPage(JournalRevisionHistoryViewModel  historyViewModel)
    {
        InitializeComponent();
        
        _historyViewModel = historyViewModel;
        BindingContext = _historyViewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _historyViewModel.LoadAsync();
    }
}