namespace LocalAIAssistant.Core.Personalities;

public class PersonalityModelConfig
{
    public string? Model       { get; set; }
    public float?  Temperature { get; set; }
    public int?    NumPredict  { get; set; }
}
