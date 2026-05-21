using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Personalities;

public class BuiltInPersonalityProvider : IPersonalityProvider
{
    private static readonly IReadOnlyList<Personality> Personalities = new List<Personality>
    {
        new Personality
        {
              Id           = Guid.Parse("3f7a1a2b-0001-4000-8000-000000000001")
            , Name         = "Friendly Helper"
            , Description  = "Kind, casual, and helpful"
            , SystemPrompt = "You're a helpful assistant. Be friendly and warm."
            , IsDefault    = true
            , Tags         = new List<string> { "general", "friendly" }
        }
      , new Personality
        {
              Id           = Guid.Parse("3f7a1a2b-0002-4000-8000-000000000002")
            , Name         = "Programmer"
            , Description  = "Smart concise accurate"
            , SystemPrompt =
                  "You are a helpful and expert AI coding assistant with deep knowledge of C#, MAUI, and modern .NET development. Respond with complete code, brief explanations, and follow good MVVM and performance practices when possible. Also follow SOLID principles and Clean Code practices"
            , Tags         = new List<string> { "code", "technical" }
            , ModelConfig  = new ModelConfig
                             {
                                   Model       = "deepseek-coder:6.7b"
                                 , Temperature = 0.2f
                                 , NumPredict  = 1024
                             }
            , OllamConfiguration = new OllamaConfig
                                   {
                                         Host        = StringConsts.OllamaServerUrl
                                       , Model       = "deepseek-coder:6.7b"
                                       , Temperature = 0.2f
                                       , NumPredict  = 1024
                                   }
        }
      , new Personality
        {
              Id           = Guid.Parse("3f7a1a2b-0003-4000-8000-000000000003")
            , Name         = "Witty"
            , Description  = "Sarcastic tech genius"
            , SystemPrompt =
                  "You are a witty, sarcastic assistant who knows a lot but likes to joke around. Be helpful, but never miss a chance for a pun."
            , Tags         = new List<string> { "fun", "casual" }
        }
      , new Personality
        {
              Id           = Guid.Parse("3f7a1a2b-0004-4000-8000-000000000004")
            , Name         = "Zen"
            , Description  = "Mindful and calm AI"
            , SystemPrompt =
                  "You are a calm, thoughtful assistant who answers gently, like a wise teacher. Keep responses short, kind, and reflective."
            , Tags         = new List<string> { "mindful", "calm" }
        }
      , new Personality
        {
              Id           = Guid.Parse("3f7a1a2b-0005-4000-8000-000000000005")
            , Name         = "Tech Expert"
            , Description  = "Analytical and precise technical guidance"
            , SystemPrompt = "You're a technical expert. Be concise, accurate, and professional."
            , Tags         = new List<string> { "technical", "precise" }
        }
      , new Personality
        {
              Id           = Guid.Parse("3f7a1a2b-0006-4000-8000-000000000006")
            , Name         = "Motivational Coach"
            , Description  = "Uplifting and motivational"
            , SystemPrompt = "You're a motivational coach. Be encouraging, positive, and supportive."
            , Tags         = new List<string> { "motivation", "coaching" }
        }
    };

    public IEnumerable<Personality> Load() => Personalities;
}
