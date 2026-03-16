using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenVEPA.Skills.Runtime;

/// <summary>
/// Discovers skill directories by scanning subdirectories for SKILL.md files.
/// Each subdirectory containing a SKILL.md is treated as a skill root.
/// </summary>
public sealed class SkillDirectory
{
    private const string SkillManifestFileName = "SKILL.md";

    private readonly SkillsOptions _options;
    private readonly ILogger<SkillDirectory> _logger;

    public SkillDirectory(IOptions<SkillsOptions> options, ILogger<SkillDirectory> logger)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Discovers all skill directories under the configured skills root.
    /// Returns a list of (directoryPath, skillMdPath) tuples for each valid skill.
    /// </summary>
    public IReadOnlyList<DiscoveredSkill> Discover()
    {
        var rootPath = Path.GetFullPath(_options.SkillsDirectory);
        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("Skills directory does not exist: {Path}", rootPath);
            return [];
        }

        var results = new List<DiscoveredSkill>();
        foreach (var subDir in Directory.EnumerateDirectories(rootPath))
        {
            var skillMdPath = Path.Combine(subDir, SkillManifestFileName);
            if (!File.Exists(skillMdPath))
            {
                _logger.LogDebug("Skipping directory without SKILL.md: {Path}", subDir);
                continue;
            }

            var skillName = Path.GetFileName(subDir);
            results.Add(new DiscoveredSkill(skillName, subDir, skillMdPath));
            _logger.LogDebug("Discovered skill: {Name} at {Path}", skillName, subDir);
        }

        _logger.LogInformation("Discovered {Count} skill(s) in {Root}", results.Count, rootPath);
        return results;
    }
}

/// <summary>
/// Represents a skill directory discovered during scanning.
/// </summary>
/// <param name="Name">Directory name used as the skill identifier during discovery.</param>
/// <param name="DirectoryPath">Full path to the skill subdirectory.</param>
/// <param name="SkillMdPath">Full path to the SKILL.md file within the subdirectory.</param>
public sealed record DiscoveredSkill(string Name, string DirectoryPath, string SkillMdPath);
