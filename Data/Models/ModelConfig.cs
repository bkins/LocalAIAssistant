namespace LocalAIAssistant.Data.Models;

public class ModelConfig
{
    public string? Model       { get; set; }
    public float?  Temperature { get; set; }
    public int?    NumPredict  { get; set; }
}