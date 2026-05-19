namespace LocalAIAssistant.Core.Personalities;

public class PersonalityCatalog
{
    private static readonly PersonalityRecord BuiltInFallback = new()
    {
          Name         = "Friendly Helper"
        , Description  = "Kind, casual, and helpful"
        , SystemPrompt = "You're a helpful assistant. Be friendly and warm."
        , IsDefault    = true
    };

    private readonly List<PersonalityRecord> _records;

    public PersonalityRecord Current { get; private set; }

    public PersonalityCatalog(IEnumerable<PersonalityRecord> records)
    {
        _records = records.ToList();
        Current  = PickDefault();
    }

    public IReadOnlyList<PersonalityRecord> GetAll() => _records.AsReadOnly();

    public PersonalityRecord? SelectById(Guid id)
        => _records.FirstOrDefault(record => record.Id == id);

    public PersonalityRecord? SelectByName(string name)
        => _records.FirstOrDefault(record => string.Equals(record.Name, name, StringComparison.OrdinalIgnoreCase));

    public bool SetCurrent(Guid id)
    {
        var match = SelectById(id);
        if (match == null) return false;

        Current = match;
        return true;
    }

    public bool SetCurrent(string name)
    {
        var match = SelectByName(name);
        if (match == null) return false;

        Current = match;
        return true;
    }

    private PersonalityRecord PickDefault()
    {
        if (_records.Count == 0)
            return BuiltInFallback;

        return _records.FirstOrDefault(record => record.IsDefault)
            ?? _records.First();
    }
}
