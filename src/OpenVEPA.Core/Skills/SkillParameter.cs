namespace OpenVEPA.Core.Skills;

/// <summary>Describes a single input parameter accepted by a skill.</summary>
public sealed record SkillParameter(
    string Name,
    string Type,
    string Description,
    bool Required,
    object? DefaultValue);
