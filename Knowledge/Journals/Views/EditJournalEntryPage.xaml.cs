using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalAIAssistant.Knowledge.Journals.ViewModels;

namespace LocalAIAssistant.Knowledge.Journals.Views;

public partial class EditJournalEntryPage : ContentPage
{
    private readonly EditJournalEntryViewModel _editJournalEntryViewModel;

    public EditJournalEntryPage(EditJournalEntryViewModel editJournalEntryViewModel)
    {
        InitializeComponent();
        BindingContext = _editJournalEntryViewModel = editJournalEntryViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _editJournalEntryViewModel.LoadAsync();
    }
}
