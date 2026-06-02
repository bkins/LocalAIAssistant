namespace LocalAIAssistant.Core.BrainDump;

public record BrainDumpCategoryDefinition(
    BrainDumpCategoryField Field
  , string                 Label
  , string                 Prompt
);

public static class BrainDumpCategories
{
    public static readonly IReadOnlyList<BrainDumpCategoryDefinition> All = new[]
    {
        new BrainDumpCategoryDefinition(
            BrainDumpCategoryField.Avoidance
          , "Things You're Putting Off"
          , "**1 of 7 — Things You're Putting Off**\nWhat are you avoiding? What have you been meaning to do? What are you dreading?"
        )
      , new BrainDumpCategoryDefinition(
            BrainDumpCategoryField.Fears
          , "Fears and Concerns"
          , "**2 of 7 — Fears and Concerns**\nWhat are you afraid of? What worries you? What keeps you up at night?"
        )
      , new BrainDumpCategoryDefinition(
            BrainDumpCategoryField.Frustrations
          , "Anger and Frustrations"
          , "**3 of 7 — Anger and Frustrations**\nWhat is making you angry or frustrated? What feels unfair or isn't working?"
        )
      , new BrainDumpCategoryDefinition(
            BrainDumpCategoryField.Discouragements
          , "Discouragements"
          , "**4 of 7 — Discouragements**\nWhat is discouraging you? Where do you feel like you're failing or falling behind?"
        )
      , new BrainDumpCategoryDefinition(
            BrainDumpCategoryField.GoalsAndBarriers
          , "Goals and Barriers"
          , "**5 of 7 — Goals and Barriers**\nWhat do you want to accomplish? What is stopping you from getting there?"
        )
      , new BrainDumpCategoryDefinition(
            BrainDumpCategoryField.HurtAndSorrow
          , "Hurt and Sorrow"
          , "**6 of 7 — Hurt and Sorrow**\nWho or what has hurt you? What grief or sadness are you carrying?"
        )
      , new BrainDumpCategoryDefinition(
            BrainDumpCategoryField.SelfCriticism
          , "Self-Criticism"
          , "**7 of 7 — Self-Criticism**\nWhat are you beating yourself up about? What do you wish you had done differently?"
        )
    };
}
