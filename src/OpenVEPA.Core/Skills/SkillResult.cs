namespace OpenVEPA.Core.Skills;

/// <summary>Encapsulates the outcome of a skill execution.</summary>
public sealed record SkillResult(
    bool Success,
    IReadOnlyDictionary<string, object?>? Data,
    SkillError? Error,
    TokenUsage? TokenUsage);
