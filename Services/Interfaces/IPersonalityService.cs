using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Services.Interfaces;

public interface IPersonalityService
{
    Personality Current { get; }
    void        SetCurrent(Personality personality);
    void        SetCurrent(string name);
    List<Personality> GetAll();
    Personality?      FindBestMatch(string emotionOrContext);
    void              Add(Personality personality); // AI-created personalities can call this
}