using CommunityToolkit.Mvvm.ComponentModel;

namespace LocalAIAssistant.Knowledge.Inbox;

public sealed partial class FilterChip : ObservableObject
{
    [ObservableProperty] private bool _isSelected;

    public string Label { get; }
    public string Value { get; }

    public FilterChip(string label, string value, bool isSelected = false)
    {
        Label       = label;
        Value       = value;
        _isSelected = isSelected;
    }
}
