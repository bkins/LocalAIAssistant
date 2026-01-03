using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.Services;

public class PersonalityService : IPersonalityService
{
    private readonly List<Personality>   _personalities;
    private readonly OllamaConfigService _ollamaConfigService;

    public Personality Current { get; private set; }

    public PersonalityService(OllamaConfigService ollamaConfigService)
    {
        _ollamaConfigService = ollamaConfigService;
        _personalities = new List<Personality>
                         {
                             new Personality
                             {
                                 Name         = "Friendly Helper"
                               , Description  = "Kind, casual, and helpful"
                               , SystemPrompt = "You're a helpful assistant. Be friendly and warm."
                               , IsDefault    = true
                             }
                           , new Personality
                             {
                                 Name        = "Programmer"
                               , Description = "Smart concise accurate"
                               , SystemPrompt =
                                     "You are a helpful and expert AI coding assistant with deep knowledge of C#, MAUI, and modern .NET development. Respond with complete code, brief explanations, and follow good MVVM and performance practices when possible. Also follow SOLID principles and Clean Code practices"
                               , OllamConfiguration = new OllamaConfig
                                                      {
                                                          Host        = StringConsts.OllamaServerUrl
                                                        , Model       = "deepseek-coder:6.7b"
                                                        , Temperature = 0.2f
                                                        , NumPredict  = 1024
                                                      }
                                 /*
                                  * "title": "Deepseek (Ollama)",
          "provider": "ollama",
          "model": "deepseek-coder:6.7b",
          "apiBase": "http://localhost:11434",
          "systemMessage": "You are a helpful and expert AI coding assistant with deep knowledge of C#, MAUI, and modern .NET development. Respond with complete code, brief explanations, and follow good MVVM and performance practices when possible. Also follow SOLID principles and Clean Code practices"
          "programmer": {
                "system_prompt": "You are 'Programmer,' an experienced, logical, and literal code expert. You answer questions with clean, efficient code and helpful comments. Never generate anything other than code.",
                "options": {"temperature": 0.2, "num_predict": 1024}
                                  */
                             }
                           , new Personality
                             {
                                 Name        = "Witty"
                               , Description = "Sarcastic tech genius"
                               , SystemPrompt =
                                     "You are a witty, sarcastic assistant who knows a lot but likes to joke around. Be helpful, but never miss a chance for a pun."
                             }
                           , new Personality
                             {
                                 Name        = "Zen"
                               , Description = "Mindful and calm AI"
                               , SystemPrompt =
                                     "You are a calm, thoughtful assistant who answers gently, like a wise teacher. Keep responses short, kind, and reflective."
                             }
                           , new Personality
                             {
                                 Name         = "Tech Expert"
                               , Description  = "Analytical and precise technical guidance"
                               , SystemPrompt = "You're a technical expert. Be concise, accurate, and professional."
                             }
                           , new Personality
                             {
                                 Name         = "Motivational Coach"
                               , Description  = "Uplifting and motivational"
                               , SystemPrompt = "You're a motivational coach. Be encouraging, positive, and supportive."
                             }

                           , new Personality
                             {
                                 Name         = StringConsts.RoleplayName
                               , Description  = StringConsts.RoleplayDescription
                               , SystemPrompt = StringConsts.RoleplaySystemPrompt
                               , OllamConfiguration = new OllamaConfig
                                                      {
                                                          Host        = StringConsts.OllamaServerUrl
                                                        , Model       = "mistral-openorca"
                                                        , Temperature = 0.9f // (0.6–0.9)
                                                        , NumPredict  = 500  // 100–500 or -2
                                                      }
                                 /*
                                  * General-purpose chatbot conversations
                                  * Goal: Create engaging, balanced, and coherent conversational responses.
                                  * temperature: Moderate (0.6–0.9).
                                  * Effect: Strikes a balance between predictability and creativity, preventing repetitive replies while still sounding natural.
                                  * num_predict: Moderate (e.g., 100–500).
                                  * Effect: Limits the length of each turn in the conversation, keeping it concise and on-topic. Use -2 if you want the response to fill the available context in a long conversation.
                                  * Example: Answering general questions, role-playing, or providing friendly advice.
                                  */
                             }
                         };

        Current = _personalities[0];
        //Current = _personalities.First(person=>person.Name == "Roleplay");
    }

    public void SetCurrent(Personality personality)
    {
        Current =  personality;
    }

    List<Personality> IPersonalityService.GetAll()
    {
        return _personalities;
    }

    public Personality? FindBestMatch(string emotionOrContext)
    {
        return _personalities.FirstOrDefault(personality => personality.Description != null 
                                                         && (personality.Description.Contains(emotionOrContext) 
                                                             || personality.Name.Contains(emotionOrContext)));
    }

    public void Add(Personality personality)
    {
        _personalities.Add(personality);
    }

    public IEnumerable<Personality> GetAll() => _personalities;

    public void SetCurrent(string name)
    {
        var found = _personalities.FirstOrDefault(personality => personality.Name == name);
        
        if (found == null) return;
        
        Current = found;
        _ollamaConfigService.UpdateConfig(Current.OllamConfiguration);
    }
}