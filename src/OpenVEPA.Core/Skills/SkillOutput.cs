namespace OpenVEPA.Core.Skills;

/// <summary>Describes a single output value produced by a skill.</summary>
public sealed record SkillOutput(
    string Name,
    string Type,
    string Description);
