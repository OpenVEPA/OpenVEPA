using Microsoft.Extensions.Logging;

using OpenVEPA.Core.Skills;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenVEPA.Skills.Runtime;

/// <summary>
/// Parses SKILL.md files to extract YAML frontmatter and build <see cref="SkillManifest"/> instances.
/// Frontmatter is delimited by <c>---</c> lines at the start of the file.
/// </summary>
public sealed class SkillMdParser
{
    private readonly ILogger<SkillMdParser> _logger;
    private readonly IDeserializer _deserializer;

    public SkillMdParser(ILogger<SkillMdParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Parses the SKILL.md file at <paramref name="skillMdPath"/> and returns a manifest,
    /// or <c>null</c> if the file is missing, malformed, or lacks required fields.
    /// </summary>
    public SkillManifest? Parse(string skillMdPath)
    {
        if (!File.Exists(skillMdPath))
        {
            _logger.LogWarning("SKILL.md not found at {Path}", skillMdPath);
            return null;
        }

        string content;
        try
        {
            content = File.ReadAllText(skillMdPath);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read SKILL.md at {Path}", skillMdPath);
            return null;
        }

        var frontmatter = ExtractFrontmatter(content);
        if (frontmatter is null)
        {
            _logger.LogWarning("No YAML frontmatter found in {Path}", skillMdPath);
            return null;
        }

        return BuildManifest(frontmatter, skillMdPath);
    }

    private string? ExtractFrontmatter(string content)
    {
        if (!content.StartsWith("---", StringComparison.Ordinal))
        {
            return null;
        }

        var endIndex = content.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return null;
        }

        // Skip the first "---\n" and extract up to the closing "---"
        var startIndex = content.IndexOf('\n', 0) + 1;
        return content[startIndex..endIndex];
    }

    private SkillManifest? BuildManifest(string yaml, string skillMdPath)
    {
        Dictionary<string, object?> data;
        try
        {
            data = _deserializer.Deserialize<Dictionary<string, object?>>(yaml)
                ?? [];
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            _logger.LogWarning(ex, "Invalid YAML frontmatter in {Path}", skillMdPath);
            return null;
        }

        var name = GetStringValue(data, "name");
        if (name is null)
        {
            _logger.LogWarning("Missing required 'name' field in {Path}", skillMdPath);
            return null;
        }

        var description = GetStringValue(data, "description") ?? string.Empty;
        var version = ResolveVersion(data);
        var skillType = ResolveSkillType(data);
        var inputs = ParseParameters(data);
        var outputs = ParseOutputs(data);
        var permissions = ParsePermissions(data);

        return new SkillManifest(name, description, version, skillType, inputs, outputs, permissions);
    }

    private static string ResolveVersion(Dictionary<string, object?> data)
    {
        // Check top-level "version" first, then "metadata.version"
        var version = GetStringValue(data, "version");
        if (version is not null)
        {
            return version;
        }

        if (data.TryGetValue("metadata", out var metaObj) && metaObj is Dictionary<object, object?> meta)
        {
            if (meta.TryGetValue("version", out var metaVersion) && metaVersion is not null)
            {
                return metaVersion.ToString() ?? "0.0.0";
            }
        }

        return "0.0.0";
    }

    private static SkillType ResolveSkillType(Dictionary<string, object?> data)
    {
        var explicitType = GetStringValue(data, "openvepa-type");
        if (string.Equals(explicitType, "native", StringComparison.OrdinalIgnoreCase))
        {
            return SkillType.Native;
        }

        if (string.Equals(explicitType, "mcp-bridge", StringComparison.OrdinalIgnoreCase))
        {
            return SkillType.McpBridge;
        }

        // Presence of dotnet assembly field implies native
        if (data.ContainsKey("openvepa-dotnet-assembly"))
        {
            return SkillType.Native;
        }

        return SkillType.McpBridge;
    }

    private static IReadOnlyList<SkillParameter> ParseParameters(Dictionary<string, object?> data)
    {
        if (!data.TryGetValue("inputs", out var inputsObj) || inputsObj is not IList<object?> inputsList)
        {
            return [];
        }

        var result = new List<SkillParameter>();
        foreach (var item in inputsList)
        {
            if (item is not Dictionary<object, object?> paramDict)
            {
                continue;
            }

            var paramName = GetStringFromDict(paramDict, "name");
            if (paramName is null)
            {
                continue;
            }

            var paramType = GetStringFromDict(paramDict, "type") ?? "string";
            var paramDesc = GetStringFromDict(paramDict, "description") ?? string.Empty;
            var required = GetBoolFromDict(paramDict, "required");
            var defaultValue = paramDict.TryGetValue("default", out var def) ? def : null;

            result.Add(new SkillParameter(paramName, paramType, paramDesc, required, defaultValue));
        }

        return result;
    }

    private static IReadOnlyList<SkillOutput> ParseOutputs(Dictionary<string, object?> data)
    {
        if (!data.TryGetValue("outputs", out var outputsObj) || outputsObj is not IList<object?> outputsList)
        {
            return [];
        }

        var result = new List<SkillOutput>();
        foreach (var item in outputsList)
        {
            if (item is not Dictionary<object, object?> outputDict)
            {
                continue;
            }

            var outputName = GetStringFromDict(outputDict, "name");
            if (outputName is null)
            {
                continue;
            }

            var outputType = GetStringFromDict(outputDict, "type") ?? "string";
            var outputDesc = GetStringFromDict(outputDict, "description") ?? string.Empty;

            result.Add(new SkillOutput(outputName, outputType, outputDesc));
        }

        return result;
    }

    private static SkillPermissions? ParsePermissions(Dictionary<string, object?> data)
    {
        if (!data.TryGetValue("permissions", out var permObj) || permObj is not Dictionary<object, object?> permDict)
        {
            return null;
        }

        var allowedTools = GetStringListFromDict(permDict, "allowed-tools");
        var requiredCapabilities = GetStringListFromDict(permDict, "required-capabilities");

        if (allowedTools is null && requiredCapabilities is null)
        {
            return null;
        }

        return new SkillPermissions(allowedTools, requiredCapabilities);
    }

    private static string? GetStringValue(Dictionary<string, object?> data, string key)
    {
        return data.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static string? GetStringFromDict(Dictionary<object, object?> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static bool GetBoolFromDict(Dictionary<object, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            return false;
        }

        return value is bool b ? b : string.Equals(value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string>? GetStringListFromDict(Dictionary<object, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value is not IList<object?> list)
        {
            return null;
        }

        var result = new List<string>();
        foreach (var item in list)
        {
            if (item?.ToString() is { } s)
            {
                result.Add(s);
            }
        }

        return result.Count > 0 ? result : null;
    }
}
