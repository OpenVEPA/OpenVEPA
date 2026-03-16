namespace OpenVEPA.Skills.Runtime;

/// <summary>
/// Configuration options for the skills runtime.
/// Bound from the "Skills" configuration section.
/// </summary>
public sealed class SkillsOptions
{
    /// <summary>
    /// Gets or sets the root directory where skill subdirectories are discovered.
    /// Each subdirectory must contain a SKILL.md file to be recognized as a skill.
    /// </summary>
    public string SkillsDirectory { get; set; } = "skills";
}
