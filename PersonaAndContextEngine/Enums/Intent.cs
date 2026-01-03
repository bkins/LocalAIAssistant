using System.ComponentModel;

namespace LocalAIAssistant.PersonaAndContextEngine.Enums;

/// <summary>
/// High-level classification of user intent.
/// Extendable as new capabilities/personas are added.
/// </summary>
public enum Intent
{
    [Description("TechnicalHelper")]
    TechnicalHelp,

    [Description("LeadershipCoach")]
    Leadership,

    [Description("Motivator")]
    Motivation,

    [Description("GeneralHelper")]
    GeneralHelp,

    [Description("Unknown")]
    Unknown
}