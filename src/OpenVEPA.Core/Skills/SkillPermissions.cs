namespace OpenVEPA.Core.Skills;

/// <summary>Declares the tools and capabilities a skill requires.</summary>
public sealed record SkillPermissions(
    IReadOnlyList<string>? AllowedTools,
    IReadOnlyList<string>? RequiredCapabilities);
