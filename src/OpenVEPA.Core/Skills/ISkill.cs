namespace OpenVEPA.Core.Skills;

/// <summary>Represents a single executable skill with metadata and lifecycle.</summary>
public interface ISkill : IAsyncDisposable
{
    /// <summary>Gets the manifest describing this skill.</summary>
    SkillManifest Manifest { get; }

    /// <summary>Executes the skill with the supplied input and context.</summary>
    Task<SkillResult> ExecuteAsync(SkillInput input, SkillExecutionContext context, CancellationToken ct);
}
