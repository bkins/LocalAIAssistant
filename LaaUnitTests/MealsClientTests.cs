using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using Moq;

namespace LaaUnitTests;

public class MealsClientTests
{
    private readonly Mock<ILoggingService> _loggingMock = new();

    [Fact]
    public async Task GetTodayAsync_ReturnsMeals_WhenSuccessful()
    {
        var meals = new List<MealDto>
        {
            new()
            {
                Id       = Guid.NewGuid().ToString()
              , MealType = "Breakfast"
              , Foods    = [new FoodEntryDto { Name = "Breakfast Scramble", Nutrition = new NutritionalInfoDto { Calories = 450 } }]
            }
        };
        var json = JsonSerializer.Serialize(meals);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetTodayAsync();

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Breakfast", result[0].MealType);
        Assert.Equal("Breakfast Scramble", result[0].Foods[0].Name);
    }

    [Fact]
    public async Task GetTodayAsync_ReturnsEmptyListAndLogs_WhenExceptionThrown()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.GetTodayAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
        _loggingMock.Verify(log => log.LogWarning(It.Is<string>(msg => msg.Contains("GetTodayAsync failed")), Category.CognitivePlatformClient), Times.Once);
    }

    [Fact]
    public async Task GetRangeAsync_ReturnsMeals_WhenSuccessful()
    {
        var meals = new List<MealDto>
        {
            new() { Id = Guid.NewGuid().ToString(), MealType = "Lunch", Foods = [new FoodEntryDto { Name = "Salad" }] },
            new() { Id = Guid.NewGuid().ToString(), MealType = "Dinner", Foods = [new FoodEntryDto { Name = "Salmon" }] }
        };
        var json = JsonSerializer.Serialize(meals);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetRangeAsync(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsSummary_WhenSuccessful()
    {
        var summary = new NutritionSummaryDto
        {
            TotalCalories = 1800,
            TotalProteinGrams = 120
        };
        var json = JsonSerializer.Serialize(summary);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetSummaryAsync(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

        Assert.NotNull(result);
        Assert.Equal(1800, result!.TotalCalories);
    }

    [Fact]
    public async Task LogMealAsync_ReturnsMeal_WhenCreated()
    {
        var inputMeal = new MealDto
        {
            Id       = Guid.NewGuid().ToString()
          , MealType = "Snack"
          , Foods    = [new FoodEntryDto { Name = "Nuts", Nutrition = new NutritionalInfoDto { Calories = 200 } }]
        };
        var json = JsonSerializer.Serialize(inputMeal);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.LogMealAsync(inputMeal);

        Assert.NotNull(result);
        Assert.Equal("Snack", result!.MealType);
    }

    [Fact]
    public async Task DeleteMealAsync_ReturnsTrue_WhenSuccess()
    {
        var sut    = BuildClient(HttpStatusCode.OK, string.Empty);
        var result = await sut.DeleteMealAsync(Guid.NewGuid());

        Assert.True(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private MealsClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new MealsClient(client, _loggingMock.Object);
    }

    private MealsClient BuildThrowingClient()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        };
        return new MealsClient(client, _loggingMock.Object);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string         _content;

        public StubHttpMessageHandler(HttpStatusCode status, string content)
        {
            _status  = status;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Simulated network failure");
    }
}
