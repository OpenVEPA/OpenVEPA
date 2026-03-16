namespace OpenVEPA.Core.Skills;

/// <summary>Manages skill registration, discovery, and execution.</summary>
public interface ISkillRuntime
{
    /// <summary>Lists manifests for all registered skills.</summary>
    IReadOnlyList<SkillManifest> ListSkills();

    /// <summary>Gets a skill by name, or null if not found.</summary>
    ISkill? GetSkill(string name);

    /// <summary>Executes the named skill with the supplied input and context.</summary>
    Task<SkillResult> ExecuteAsync(string skillName, SkillInput input, SkillExecutionContext context, CancellationToken ct);
}
