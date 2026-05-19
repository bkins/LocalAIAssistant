using LocalAIAssistant.Core.Personalities;

namespace LaaUnitTests;

public class PersonalityCatalogTests
{
    private static PersonalityRecord Record(string name, bool isDefault = false, Guid? id = null) =>
        new()
        {
              Id        = id ?? Guid.NewGuid()
            , Name      = name
            , IsDefault = isDefault
        };

    [Fact]
    public void Current_IsBuiltInFallback_When_No_Records_Provided()
    {
        var catalog = new PersonalityCatalog(Array.Empty<PersonalityRecord>());

        Assert.NotNull(catalog.Current);
        Assert.Equal("Friendly Helper", catalog.Current.Name);
        Assert.True(catalog.Current.IsDefault);
    }

    [Fact]
    public void Current_IsFirst_Record_When_No_Record_IsMarkedDefault()
    {
        var first  = Record("Alpha");
        var second = Record("Beta");
        var catalog = new PersonalityCatalog(new[] { first, second });

        Assert.Equal("Alpha", catalog.Current.Name);
    }

    [Fact]
    public void Current_IsMarkedDefault_Record_When_Present()
    {
        var first   = Record("Alpha");
        var defaultOne = Record("Beta", isDefault: true);
        var third   = Record("Gamma");

        var catalog = new PersonalityCatalog(new[] { first, defaultOne, third });

        Assert.Equal("Beta", catalog.Current.Name);
    }

    [Fact]
    public void SelectById_Returns_Matching_Record()
    {
        var targetId = Guid.Parse("3f7a1a2b-0001-4000-8000-000000000001");
        var target   = Record("Target", id: targetId);
        var catalog  = new PersonalityCatalog(new[] { Record("Other"), target });

        var result = catalog.SelectById(targetId);

        Assert.NotNull(result);
        Assert.Equal("Target", result!.Name);
    }

    [Fact]
    public void SelectById_Returns_Null_When_Id_Not_Found()
    {
        var catalog = new PersonalityCatalog(new[] { Record("Solo") });

        var result = catalog.SelectById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void SelectByName_Returns_Matching_Record_CaseInsensitive()
    {
        var catalog = new PersonalityCatalog(new[] { Record("Programmer"), Record("Zen") });

        var result = catalog.SelectByName("programmer");

        Assert.NotNull(result);
        Assert.Equal("Programmer", result!.Name);
    }

    [Fact]
    public void SelectByName_Returns_Null_When_Name_Not_Found()
    {
        var catalog = new PersonalityCatalog(new[] { Record("Alpha") });

        var result = catalog.SelectByName("Nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void SetCurrent_ById_Updates_Current_And_Returns_True()
    {
        var targetId = Guid.Parse("3f7a1a2b-0002-4000-8000-000000000002");
        var target   = Record("Programmer", id: targetId);
        var catalog  = new PersonalityCatalog(new[] { Record("Friendly Helper", isDefault: true), target });

        var success = catalog.SetCurrent(targetId);

        Assert.True(success);
        Assert.Equal("Programmer", catalog.Current.Name);
    }

    [Fact]
    public void SetCurrent_ById_Returns_False_And_Leaves_Current_Unchanged_When_Id_Missing()
    {
        var catalog = new PersonalityCatalog(new[] { Record("Only One", isDefault: true) });

        var success = catalog.SetCurrent(Guid.NewGuid());

        Assert.False(success);
        Assert.Equal("Only One", catalog.Current.Name);
    }

    [Fact]
    public void SetCurrent_ByName_Updates_Current_And_Returns_True()
    {
        var catalog = new PersonalityCatalog(new[] { Record("Friendly Helper", isDefault: true), Record("Zen") });

        var success = catalog.SetCurrent("Zen");

        Assert.True(success);
        Assert.Equal("Zen", catalog.Current.Name);
    }

    [Fact]
    public void GetAll_Returns_All_Provided_Records()
    {
        var records = new[] { Record("A"), Record("B"), Record("C") };
        var catalog = new PersonalityCatalog(records);

        var all = catalog.GetAll();

        Assert.Equal(3, all.Count);
        Assert.Equal(new[] { "A", "B", "C" }, all.Select(record => record.Name));
    }
}
