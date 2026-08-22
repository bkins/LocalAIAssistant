using System;
using LocalAIAssistant.ViewModels;
using Microsoft.Maui.Controls;

namespace LocalAIAssistant.Views;

public partial class RecordingsPage : ContentPage
{
    private readonly RecordingsViewModel _viewModel;

    public RecordingsPage(RecordingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }
}
