using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.Personalities;

public class SimplePersonalitySelector : IPersonalitySelector
{
    public Personality? Select(string input, IEnumerable<Personality> personalities)
    {
        input = input.ToLowerInvariant();

        return personalities
              .Select(p => new
                           {
                                   Personality = p,
                                   Score       = Score(input, p)
                           })
              .OrderByDescending(x => x.Score)
              .FirstOrDefault()?.Personality;
    }

    private int Score(string input, Personality p)
    {
        int score = 0;

        if (p.Name.ToLower().Contains(input))
            score += 5;

        if (p.Description.ToLower().Contains(input))
            score += 3;

        foreach (var tag in p.Tags)
        {
            if (input.Contains(tag.ToLower()))
                score += 4;
        }

        return score;
    }
}
