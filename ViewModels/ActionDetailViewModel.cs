using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.DTOs;
using Microsoft.Maui.Controls;

namespace LocalAIAssistant.ViewModels;

public partial class ActionDetailViewModel : ObservableObject
{
    private readonly ChatViewModel _chatViewModel;

    [ObservableProperty] private ActionMetadataDto action = null!;
    public IEnumerable<ExampleItem> Examples => action?.Examples?.Select(e => new ExampleItem
                                                                              {
                                                                                      Text = e
                                                                              }) ?? Enumerable.Empty<ExampleItem>();
    public bool HasExamples   => Action?.Examples?.Length > 0;
    public bool HasParameters => Action?.Parameters?.Count > 0;

    public ActionDetailViewModel(ChatViewModel chatViewModel)
    {
        _chatViewModel = chatViewModel;
    }

    public void Load(ActionMetadataDto action)
    {
        Action = action;

        OnPropertyChanged(nameof(Examples));
        OnPropertyChanged(nameof(HasExamples));
        OnPropertyChanged(nameof(HasParameters));
    }

    [RelayCommand]
    private async Task TryActionAsync()
    {
        if (Action == null)
            return;

        if (Action.Examples is { Length: > 0 } examples)
        {
            _chatViewModel.PromptText = examples[0];
        }
        else
        {
            _chatViewModel.PromptText = Action.Name;
        }

        await Shell.Current.GoToAsync("//Chat");
    }
}