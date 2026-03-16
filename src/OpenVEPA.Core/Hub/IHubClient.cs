using OpenVEPA.Core.Skills;

namespace OpenVEPA.Core.Hub;

/// <summary>Discovers and installs skills from a remote hub registry.</summary>
public interface IHubClient
{
    /// <summary>Searches the hub for skills matching the query.</summary>
    Task<IReadOnlyList<SkillManifest>> SearchAsync(string query, CancellationToken ct);

    /// <summary>Installs a skill by name and version, returning true on success.</summary>
    Task<bool> InstallAsync(string skillName, string version, CancellationToken ct);

    /// <summary>Gets whether the hub is reachable.</summary>
    bool IsAvailable { get; }
}
