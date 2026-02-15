using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using Message = LocalAIAssistant.Data.Models.Message;

namespace LocalAIAssistant.ViewModels;

public partial class MemoryManagementViewModel : ObservableObject
{
    private readonly IConversationMemory _conversationMemory;

    [ObservableProperty]
    private ObservableCollection<Message> _shortTermMessages = new();

    [ObservableProperty]
    private ObservableCollection<Message> _longTermMessages = new();

    public int ShortTermCount => ShortTermMessages.Count;
    public int LongTermCount  => LongTermMessages.Count;
    
    public MemoryManagementViewModel(IConversationMemory conversationMemory)
    {
        _conversationMemory = conversationMemory;
    }
    
    [RelayCommand]
    public async Task LoadAsync()
    {
        ShortTermMessages.Clear();
        LongTermMessages.Clear();

        var shortTerm = await _conversationMemory.LoadShortTermAsync();
        foreach (var msg in shortTerm)
            ShortTermMessages.Add(msg);

        var longTerm = await _conversationMemory.LoadLongTermAsync();
        foreach (var msg in longTerm)
            LongTermMessages.Add(msg);
        
        OnPropertyChanged(nameof(ShortTermCount));
        OnPropertyChanged(nameof(LongTermCount));
    }

    [RelayCommand]
    public Task ClearSessionAsync()
    {
        return _conversationMemory.ClearAsync();
    }
    [RelayCommand]
    public async Task ClearShortTermAsync()
    {
        await _conversationMemory.ClearShortTermAsync();
        await LoadAsync();
    }

    [RelayCommand]
    public async Task ClearLongTermAsync()
    {
        await _conversationMemory.ClearLongTermAsync();
        await LoadAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadAsync();
    }

}