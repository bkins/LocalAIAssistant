namespace LocalAIAssistant.Data.Models;

public record OllamaConfig
{
    public string Model { get; set; } = "llama3";
    public int     NumPredict  { get; set; } = 128;
    public float   Temperature { get; set; } = 0.8f;

    public string Host { get; set; } = StringConsts.OllamaServerUrl;

    public override string ToString()
    {
        return $"Model: {Model}, NumPredict: {NumPredict}, Temperature: {Temperature}";
    }
}