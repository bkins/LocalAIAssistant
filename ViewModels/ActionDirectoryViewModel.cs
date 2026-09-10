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
    private const string AllCategories = "All categories";

    private readonly ICognitivePlatformClientFactory _clientFactory;
    private readonly ILoggingService                 _logger;
    private readonly ActionDetailViewModel           _detailViewModel;

    
    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _selectedCategory = AllCategories;
    [ObservableProperty] private bool   _showFastPathOnly;
    [ObservableProperty] private bool   _showConfirmationRequiredOnly;
    
    public ObservableCollection<ActionMetadataDto> AllActions { get; } = new();
    public ObservableCollection<ActionMetadataDto> FilteredActions { get; } = new();
    public ObservableCollection<string> Categories { get; } = [AllCategories];

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

            Categories.Clear();
            Categories.Add(AllCategories);
            foreach (var category in actions.Select(action => action.Category)
                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase))
            {
                Categories.Add(category);
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

    partial void OnSelectedCategoryChanged(string value)
    {
        FilterActions();
    }

    partial void OnShowFastPathOnlyChanged(bool value)
    {
        FilterActions();
    }

    partial void OnShowConfirmationRequiredOnlyChanged(bool value)
    {
        FilterActions();
    }

    private void FilterActions()
    {
        FilteredActions.Clear();
        
        var query = SearchQuery?.ToLowerInvariant().Trim() ?? string.Empty;

        var filtered = AllActions.Where(action => (query.IsNullOrEmpty()
                                                || action.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                                                || action.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                                                || action.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                                             && (SelectedCategory == AllCategories
                                                || action.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                                             && (!ShowFastPathOnly || action.IsFastPath)
                                             && (!ShowConfirmationRequiredOnly || action.IsDestructive))
                                 .OrderBy(action => action.Category, StringComparer.OrdinalIgnoreCase)
                                 .ThenBy(action => action.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var action in filtered)
        {
            FilteredActions.Add(action);
        }
    }
}
