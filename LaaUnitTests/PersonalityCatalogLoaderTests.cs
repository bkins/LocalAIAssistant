using LocalAIAssistant.Core.Personalities;

namespace LaaUnitTests;

public class PersonalityCatalogLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public PersonalityCatalogLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string WriteCatalog(string json)
    {
        var path = Path.Combine(_tempDir, "Personalities.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Load_Returns_EmptyList_When_File_Does_Not_Exist()
    {
        var path   = Path.Combine(_tempDir, "missing.json");
        var loader = new PersonalityCatalogLoader(path);

        var result = loader.Load();

        Assert.Empty(result);
    }

    [Fact]
    public void Load_Returns_EmptyList_When_File_Is_Empty_Array()
    {
        var path   = WriteCatalog("""{"personalities": []}""");
        var loader = new PersonalityCatalogLoader(path);

        var result = loader.Load();

        Assert.Empty(result);
    }

    [Fact]
    public void Load_Deserializes_All_Fields_From_Valid_Catalog()
    {
        var path = WriteCatalog("""
            {
              "personalities": [
                {
                  "id": "3f7a1a2b-0001-4000-8000-000000000001",
                  "name": "Friendly Helper",
                  "description": "Kind and helpful",
                  "systemPrompt": "Be helpful and warm.",
                  "isDefault": true,
                  "tags": ["general", "friendly"]
                }
              ]
            }
            """);

        var loader = new PersonalityCatalogLoader(path);

        var result = loader.Load();

        Assert.Single(result);

        var personality = result[0];
        Assert.Equal(Guid.Parse("3f7a1a2b-0001-4000-8000-000000000001"), personality.Id);
        Assert.Equal("Friendly Helper",  personality.Name);
        Assert.Equal("Kind and helpful", personality.Description);
        Assert.Equal("Be helpful and warm.", personality.SystemPrompt);
        Assert.True(personality.IsDefault);
        Assert.Equal(new[] { "general", "friendly" }, personality.Tags);
    }

    [Fact]
    public void Load_Deserializes_ModelConfig_When_Present()
    {
        var path = WriteCatalog("""
            {
              "personalities": [
                {
                  "id": "3f7a1a2b-0002-4000-8000-000000000002",
                  "name": "Programmer",
                  "systemPrompt": "You are an expert developer.",
                  "modelConfig": {
                    "model": "deepseek-coder:6.7b",
                    "temperature": 0.2,
                    "numPredict": 1024
                  }
                }
              ]
            }
            """);

        var loader = new PersonalityCatalogLoader(path);

        var result = loader.Load();

        Assert.Single(result);
        Assert.NotNull(result[0].ModelConfig);
        Assert.Equal("deepseek-coder:6.7b", result[0].ModelConfig!.Model);
        Assert.Equal(0.2f,                  result[0].ModelConfig!.Temperature);
        Assert.Equal(1024,                  result[0].ModelConfig!.NumPredict);
    }

    [Fact]
    public void Load_Returns_AllEntries_From_Multi_Personality_Catalog()
    {
        var path = WriteCatalog("""
            {
              "personalities": [
                { "id": "3f7a1a2b-0001-4000-8000-000000000001", "name": "Alpha", "systemPrompt": "A" },
                { "id": "3f7a1a2b-0002-4000-8000-000000000002", "name": "Beta",  "systemPrompt": "B" },
                { "id": "3f7a1a2b-0003-4000-8000-000000000003", "name": "Gamma", "systemPrompt": "C" }
              ]
            }
            """);

        var loader = new PersonalityCatalogLoader(path);

        var result = loader.Load();

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, result.Select(record => record.Name));
    }

    [Fact]
    public void Load_Is_CaseInsensitive_For_Json_Property_Names()
    {
        var path = WriteCatalog("""
            {
              "Personalities": [
                {
                  "ID": "3f7a1a2b-0001-4000-8000-000000000001",
                  "NAME": "Case Test",
                  "SystemPrompt": "Testing."
                }
              ]
            }
            """);

        var loader = new PersonalityCatalogLoader(path);

        var result = loader.Load();

        Assert.Single(result);
        Assert.Equal("Case Test", result[0].Name);
    }
}
