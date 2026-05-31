using LocalAIAssistant.Core.Personality;

namespace LocalAIAssistant.Core.IntegrationTests;

/// <summary>
/// Contract tests for <see cref="PersonalityApiClient"/> against the live CP dev API.
/// Prerequisite: CognitivePlatform.Api running at http://localhost:5273
/// </summary>
[Trait("Category", "Integration")]
public class PersonalityApiClientIntegrationTests
{
    private const string BaseUrl = "http://localhost:5273";

    private readonly ITestOutputHelper    _output;
    private readonly PersonalityApiClient _client;

    public PersonalityApiClientIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _client = new PersonalityApiClient(
                      new HttpClient { BaseAddress = new Uri(BaseUrl) });
    }

    // ── GetPersonalitiesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPersonalitiesAsync_ReturnsNonNullList_WhenApiIsReachable()
    {
        var result = await _client.GetPersonalitiesAsync();

        _output.WriteLine($"Personalities returned: {result.Count}");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPersonalitiesAsync_EachPersonality_HasNonDefaultIdAndNonEmptyName()
    {
        var result = await _client.GetPersonalitiesAsync();

        foreach (var personality in result)
        {
            _output.WriteLine($"  id={personality.Id}  name={personality.Name}  "
                            + $"active={personality.IsActive}  builtIn={personality.IsBuiltIn}");

            personality.Id.Should().NotBe(Guid.Empty,
                "each personality must carry a real server-assigned GUID — Guid.Empty signals a deserialization failure");
            personality.Name.Should().NotBeNullOrEmpty(
                "Name is the user-visible label — must never be empty");
        }
    }

    [Fact]
    public async Task GetPersonalitiesAsync_AtMostOnePersonality_IsActive()
    {
        var result      = await _client.GetPersonalitiesAsync();
        var activeCount = result.Count(personality => personality.IsActive);

        _output.WriteLine($"Active: {activeCount} of {result.Count} total personalities");

        activeCount.Should().BeLessThanOrEqualTo(1,
            "exactly one personality (or none) may be active at a time");
    }

    [Fact]
    public async Task GetPersonalitiesAsync_ModelConfig_HasTemperatureInValidRange_WhenPresent()
    {
        var result = await _client.GetPersonalitiesAsync();

        foreach (var personality in result.Where(personality => personality.ModelConfig is not null))
        {
            _output.WriteLine($"  name={personality.Name}  "
                            + $"provider={personality.ModelConfig!.Provider}  "
                            + $"model={personality.ModelConfig.ModelId}  "
                            + $"temperature={personality.ModelConfig.Temperature}");

            personality.ModelConfig!.Temperature.Should().BeInRange(0f, 2f,
                "LLM temperature must be within the valid range [0, 2]");
        }
    }

    // ── ActivateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_DoesNotThrow_WhenIdDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();

        var exception = await Record.ExceptionAsync(() => _client.ActivateAsync(nonExistentId));

        _output.WriteLine($"ActivateAsync with unknown id: exception={exception?.GetType().Name ?? "none"}");

        exception.Should().BeNull(
            "PersonalityApiClient.ActivateAsync is fire-and-forget — it must never surface exceptions to callers");
    }
}
