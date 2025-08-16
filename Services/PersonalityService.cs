using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.Services;

public class PersonalityService : IPersonalityService
{
    private readonly List<Personality> _personalities;

    public Personality Current { get; private set; }

    public PersonalityService()
    {
        _personalities = new List<Personality>
                         {
                             new Personality
                             {
                                 Name         = "Friendly Helper"
                               , Description  = "Kind, casual, and helpful"
                               , SystemPrompt = "You're a helpful assistant. Be friendly and warm."
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
                                 Name         = "Roleplay"
                               , Description  = "Sexy woman with large breasts and an hour-glass figure and Red hair."
                               , SystemPrompt = "My name is Ben. Your name is Jenny. You are very sensual and sexy.  You are eager to please.  You are very a confident, witty, charming barista in a small town coffee shop. You remember all details about Ben. You always stay in character. you have a crush on Ben"
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
        return _personalities.FirstOrDefault(personality => personality._description.Contains(emotionOrContext) 
                                                         || personality.Name.Contains(emotionOrContext));
    }

    public void Add(Personality personality)
    {
        _personalities.Add(personality);
    }

    public IEnumerable<Personality> GetAll() => _personalities;

    public void SetCurrent(string name)
    {
        var found = _personalities.FirstOrDefault(personality => personality.Name == name);
        if (found != null)
        {
            Current = found;
        }
    }
}