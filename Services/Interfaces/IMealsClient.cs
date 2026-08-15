using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LocalAIAssistant.CognitivePlatform.DTOs;

namespace LocalAIAssistant.Services.Interfaces;

public interface IMealsClient
{
    Task<IReadOnlyList<MealDto>> GetTodayAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MealDto>> GetRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<NutritionSummaryDto?>   GetSummaryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<MealDto?>               LogMealAsync(MealDto meal, CancellationToken ct = default);
    Task<bool>                   DeleteMealAsync(Guid id, CancellationToken ct = default);
}
