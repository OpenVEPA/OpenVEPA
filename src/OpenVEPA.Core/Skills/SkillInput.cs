namespace OpenVEPA.Core.Skills;

/// <summary>Carries the parameters and prompt supplied to a skill execution.</summary>
public sealed record SkillInput(
    string Prompt,
    IReadOnlyDictionary<string, object?> Parameters);
