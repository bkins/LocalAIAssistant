using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;

namespace LocalAIAssistant.ViewModels;

public partial class AiMemoryViewModel : ObservableObject
{
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private ObservableCollection<Message> _memoryEntries = new();

    [ObservableProperty]
    private Message _selectedEntry;

    [ObservableProperty]
    private string _editMessage;

    public AiMemoryViewModel(ILoggingService loggingService)
    {
        _loggingService = loggingService;
        LoadMemory();
    }

    private void LoadMemory()
    {
        MemoryEntries.Clear();
        // var entries = _loggingService.GetLongTermMemory(); //TODO: This will need to be a method in the AiMemory system
        // foreach (var entry in entries)
        // {
        //     MemoryEntries.Add(entry);
        // }
    }

    [RelayCommand]
    private void SelectEntry(Message entry)
    {
        SelectedEntry = entry;
        EditMessage   = entry.Content;
    }

    [RelayCommand]
    private void SaveEdit()
    {
        if (SelectedEntry == null) return;
        
        SelectedEntry.Content = EditMessage;
        //_loggingService.UpdateMessage(SelectedEntry); //TODO: This will need to be a method in the AiMemory system
        LoadMemory();
    }

    [RelayCommand]
    private void DeleteEntry(Message entry)
    {
        //_loggingService.DeleteMessage(entry); //TODO: This will need to be a method in the AiMemory system
        LoadMemory();
    }
}