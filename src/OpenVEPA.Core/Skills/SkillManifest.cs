namespace OpenVEPA.Core.Skills;

/// <summary>Metadata that describes a skill's identity, inputs, and outputs.</summary>
public sealed record SkillManifest(
    string Name,
    string Description,
    string Version,
    SkillType Type,
    IReadOnlyList<SkillParameter> Inputs,
    IReadOnlyList<SkillOutput> Outputs,
    SkillPermissions? Permissions);
