using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using OpenVEPA.Core.Skills;

namespace OpenVEPA.Skills.Runtime;

/// <summary>
/// Default implementation of <see cref="ISkillRuntime"/>.
/// Scans the skills directory on construction, caches manifests, and lazy-loads skill instances on first access.
/// </summary>
public sealed class SkillRuntime : ISkillRuntime
{
    private readonly SkillMdParser _parser;
    private readonly SkillDirectory _directory;
    private readonly NativeSkillLoader _nativeLoader;
    private readonly ILogger<SkillRuntime> _logger;

    private readonly Dictionary<string, SkillManifest> _manifests = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DiscoveredSkill> _discovered = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ISkill> _loadedSkills = new(StringComparer.OrdinalIgnoreCase);

    public SkillRuntime(
        SkillMdParser parser,
        SkillDirectory directory,
        NativeSkillLoader nativeLoader,
        ILogger<SkillRuntime> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _nativeLoader = nativeLoader ?? throw new ArgumentNullException(nameof(nativeLoader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ScanSkills();
    }

    /// <inheritdoc />
    public IReadOnlyList<SkillManifest> ListSkills()
    {
        return [.. _manifests.Values];
    }

    /// <inheritdoc />
    public ISkill? GetSkill(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (_loadedSkills.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var skill = LoadSkill(name);
        if (skill is not null)
        {
            _loadedSkills.TryAdd(name, skill);
        }

        return skill;
    }

    /// <inheritdoc />
    public async Task<SkillResult> ExecuteAsync(
        string skillName,
        SkillInput input,
        SkillExecutionContext context,
        CancellationToken ct)
    {
        var skill = GetSkill(skillName);
        if (skill is null)
        {
            return new SkillResult(
                Success: false,
                Data: null,
                Error: new SkillError(SkillErrorKind.Permanent, $"Skill '{skillName}' not found."),
                TokenUsage: null);
        }

        try
        {
            return await skill.ExecuteAsync(input, context, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new SkillResult(
                Success: false,
                Data: null,
                Error: new SkillError(SkillErrorKind.Timeout, $"Skill '{skillName}' execution was cancelled."),
                TokenUsage: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skill '{SkillName}' execution failed", skillName);
            return new SkillResult(
                Success: false,
                Data: null,
                Error: new SkillError(SkillErrorKind.Permanent, $"Skill '{skillName}' execution failed: {ex.Message}", ex),
                TokenUsage: null);
        }
    }

    private void ScanSkills()
    {
        var discovered = _directory.Discover();
        foreach (var entry in discovered)
        {
            var manifest = _parser.Parse(entry.SkillMdPath);
            if (manifest is null)
            {
                _logger.LogWarning("Skipping skill at {Path}: invalid SKILL.md", entry.DirectoryPath);
                continue;
            }

            _manifests[manifest.Name] = manifest;
            _discovered[manifest.Name] = entry;
            _logger.LogDebug("Registered skill manifest: {Name} v{Version}", manifest.Name, manifest.Version);
        }

        _logger.LogInformation("Loaded {Count} skill manifest(s)", _manifests.Count);
    }

    private ISkill? LoadSkill(string name)
    {
        if (!_manifests.TryGetValue(name, out var manifest) || !_discovered.TryGetValue(name, out var entry))
        {
            _logger.LogWarning("Skill '{Name}' not found in registry", name);
            return null;
        }

        return manifest.Type switch
        {
            SkillType.Native => LoadNativeSkill(entry, manifest),
            SkillType.McpBridge => new McpBridgeSkill(manifest),
            SkillType.Hybrid => LoadNativeSkill(entry, manifest), // Hybrid falls back to native for Phase 1
            _ => LogAndReturnNull(name, manifest.Type)
        };
    }

    private ISkill? LoadNativeSkill(DiscoveredSkill entry, SkillManifest manifest)
    {
        // Read the assembly name from the frontmatter
        var content = File.ReadAllText(entry.SkillMdPath);
        var assemblyName = ExtractAssemblyName(content);
        if (assemblyName is null)
        {
            _logger.LogError("Skill '{Name}' is native but has no 'openvepa-dotnet-assembly' in SKILL.md", manifest.Name);
            return null;
        }

        return _nativeLoader.Load(entry.DirectoryPath, assemblyName, manifest);
    }

    private static string? ExtractAssemblyName(string skillMdContent)
    {
        // Simple extraction: find the openvepa-dotnet-assembly line in frontmatter
        using var reader = new StringReader(skillMdContent);
        var line = reader.ReadLine();
        if (line is null || !line.StartsWith("---", StringComparison.Ordinal))
        {
            return null;
        }

        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("---", StringComparison.Ordinal))
            {
                break;
            }

            if (line.StartsWith("openvepa-dotnet-assembly:", StringComparison.OrdinalIgnoreCase))
            {
                return line["openvepa-dotnet-assembly:".Length..].Trim();
            }
        }

        return null;
    }

    private ISkill? LogAndReturnNull(string name, SkillType type)
    {
        _logger.LogError("Unsupported skill type '{Type}' for skill '{Name}'", type, name);
        return null;
    }
}
