using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;

namespace LocalAIAssistant.Services;

public class MealsClient : IMealsClient
{
    private readonly HttpClient      _httpClient;
    private readonly ILoggingService _loggingService;

    public MealsClient( HttpClient      httpClient
                      , ILoggingService loggingService )
    {
        _httpClient     = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    public async Task<IReadOnlyList<MealDto>> GetTodayAsync(CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<MealDto>>("api/meals/today", ct).ConfigureAwait(false)
                ?? new List<MealDto>();
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning($"GetTodayAsync failed: {ex.Message}", Category.CognitivePlatformClient);
            return Array.Empty<MealDto>();
        }
    }

    public async Task<IReadOnlyList<MealDto>> GetRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        try
        {
            var uri = $"api/meals?from={Uri.EscapeDataString(from.ToString("yyyy-MM-dd"))}&to={Uri.EscapeDataString(to.ToString("yyyy-MM-dd"))}";
            return await _httpClient.GetFromJsonAsync<List<MealDto>>(uri, ct).ConfigureAwait(false)
                ?? new List<MealDto>();
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning($"GetRangeAsync failed: {ex.Message}", Category.CognitivePlatformClient);
            return Array.Empty<MealDto>();
        }
    }

    public async Task<NutritionSummaryDto?> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        try
        {
            var uri = $"api/meals/summary?from={Uri.EscapeDataString(from.ToString("yyyy-MM-dd"))}&to={Uri.EscapeDataString(to.ToString("yyyy-MM-dd"))}";
            return await _httpClient.GetFromJsonAsync<NutritionSummaryDto>(uri, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning($"GetSummaryAsync failed: {ex.Message}", Category.CognitivePlatformClient);
            return null;
        }
    }

    public async Task<MealDto?> LogMealAsync(MealDto meal, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/meals", meal, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MealDto>(cancellationToken: ct).ConfigureAwait(false);
            }
            return null;
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning($"LogMealAsync failed: {ex.Message}", Category.CognitivePlatformClient);
            return null;
        }
    }

    public async Task<bool> DeleteMealAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/meals/{id:N}", ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning($"DeleteMealAsync failed: {ex.Message}", Category.CognitivePlatformClient);
            return false;
        }
    }
}
