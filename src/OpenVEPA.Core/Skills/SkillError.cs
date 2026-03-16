namespace OpenVEPA.Core.Skills;

/// <summary>Represents a categorized error produced during skill execution.</summary>
public sealed record SkillError(
    SkillErrorKind Kind,
    string Message,
    Exception? Inner = null);
