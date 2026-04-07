using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CP.Client.Core.Avails;
using LocalAIAssistant.Services;

namespace LocalAIAssistant.ViewModels;

/// <summary>
/// Exposes Groq rate-limit usage as bindable properties for the shell header.
/// Refresh is triggered after each conversation turn and on manual tap.
/// </summary>
public partial class UsageViewModel : ObservableObject
{
    private readonly UsageService _usageService;

    // ----------------------------------------------------------------
    // Requests
    // ----------------------------------------------------------------

    [ObservableProperty] private int    _requestsRemaining;
    [ObservableProperty] private int    _requestLimit;
    [ObservableProperty] private double _requestUsagePercent;
    [ObservableProperty] private string _requestsResetLabel = string.Empty;

    // ----------------------------------------------------------------
    // Tokens
    // ----------------------------------------------------------------

    [ObservableProperty] private int    _tokensRemaining;
    [ObservableProperty] private int    _tokenLimit;
    [ObservableProperty] private double _tokenUsagePercent;
    [ObservableProperty] private string _tokensResetLabel = string.Empty;

    // ----------------------------------------------------------------
    // Display helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Compact single-line summary for the shell header, e.g.:
    /// "847 req · 42k tok"
    /// </summary>
    [ObservableProperty] private string _headerSummary = string.Empty;

    /// <summary>True once the first successful fetch has returned data.</summary>
    [ObservableProperty] private bool _hasData;

    // ----------------------------------------------------------------
    // Warning thresholds
    // ----------------------------------------------------------------

    /// <summary>Header label turns amber when usage exceeds this percentage.</summary>
    public double WarnThresholdPercent => 70.0;

    /// <summary>Header label turns red when usage exceeds this percentage.</summary>
    public double DangerThresholdPercent => 90.0;

    [ObservableProperty] private Color _headerColor = Colors.Transparent;

    public UsageViewModel(UsageService usageService)
    {
        _usageService = usageService;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await _usageService.RefreshAsync();
        ApplySnapshot();
    }

    // Called by AppShellMasterViewModel after each conversation turn.
    public async Task RefreshAfterTurnAsync()
    {
        await _usageService.RefreshAsync();
        ApplySnapshot();
    }

    // ----------------------------------------------------------------
    // Private
    // ----------------------------------------------------------------

    private void ApplySnapshot()
    {
        var data = _usageService.Latest;

        if (data is null || data.HasData.Not())
            return;

        HasData = true;

        RequestsRemaining    = data.Requests.Remaining;
        RequestLimit         = data.Requests.Limit;
        RequestUsagePercent  = data.Requests.UsagePercent;
        RequestsResetLabel   = data.Requests.ResetApproxLocal;

        TokensRemaining      = data.Tokens.Remaining;
        TokenLimit           = data.Tokens.Limit;
        TokenUsagePercent    = data.Tokens.UsagePercent;
        TokensResetLabel     = data.Tokens.ResetApproxLocal;

        HeaderSummary = FormatHeaderSummary(data.Requests.Remaining
                                          , data.Requests.Limit
                                          , data.Tokens.Remaining
                                          , data.Tokens.Limit);

        HeaderColor = DeriveHeaderColor(data.Requests.UsagePercent
                                       , data.Tokens.UsagePercent);
    }

    private static string FormatHeaderSummary( int requestsRemaining
                                             , int requestsLimit
                                             , int tokensRemaining
                                             , int tokensLimit )
    {
        var tokLabel = tokensRemaining >= 1000
                               ? $"{tokensRemaining / 1000.0:0.#}k"
                               : tokensRemaining.ToString();
        var tokLabelLimit = tokensLimit >= 1000
                               ? $"{tokensLimit / 1000.0:0.#}k"
                               : tokensLimit.ToString();
        
        return $"{requestsRemaining}/{requestsLimit} req · {tokLabel}/{tokLabelLimit} tok";
    }

    private Color DeriveHeaderColor(double requestPercent, double tokenPercent)
    {
        var worstPercent = Math.Max(requestPercent, tokenPercent);

        if (worstPercent >= DangerThresholdPercent) return Colors.Red;
        if (worstPercent >= WarnThresholdPercent)   return Colors.Orange;

        return Colors.Gray;
    }
}