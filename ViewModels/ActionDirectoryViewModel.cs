using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using LocalAIAssistant.Views;

namespace LocalAIAssistant.ViewModels;

public partial class ActionDirectoryViewModel : ObservableObject
{
    private readonly ICognitivePlatformClientFactory _clientFactory;
    private readonly ILoggingService                 _logger;
    private readonly ActionDetailViewModel           _detailViewModel;

    
    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private string _searchQuery = string.Empty;
    
    public ObservableCollection<ActionMetadataDto> AllActions { get; } = new();
    public ObservableCollection<ActionMetadataDto> FilteredActions { get; } = new();

    public ActionDirectoryViewModel( ICognitivePlatformClientFactory clientFactory
                                   , ILoggingService                 logger
                                   , ActionDetailViewModel           detailViewModel )
    {
        _clientFactory   = clientFactory;
        _logger          = logger;
        _detailViewModel = detailViewModel;

        _ = LoadActionsAsync();
    }

    [RelayCommand]
    private async Task LoadActionsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var client = _clientFactory.Create();
            var actions = await client.GetActionsAsync();
            
            AllActions.Clear();
            foreach (var action in actions)
            {
                AllActions.Add(action);
            }
            
            FilterActions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load actions: {ex.Message}", Category.Ui);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectActionAsync(ActionMetadataDto action)
    {
        if (action is null) return;

        _detailViewModel.Load(action);

        await Shell.Current.Navigation.PushAsync(new ActionDetailPage(_detailViewModel));
    }
    
    partial void OnSearchQueryChanged(string value)
    {
        FilterActions();
    }

    private void FilterActions()
    {
        FilteredActions.Clear();
        
        var query = SearchQuery?.ToLowerInvariant().Trim() ?? string.Empty;

        var filtered = string.IsNullOrEmpty(query)
                               ? AllActions
                               : AllActions.Where(action => action.Name.ToLowerInvariant().Contains(query)
                                                         || action.Category.ToLowerInvariant().Contains(query)
                                                         || action.Description.ToLowerInvariant().Contains(query));

        foreach (var action in filtered)
        {
            FilteredActions.Add(action);
        }
    }
}
