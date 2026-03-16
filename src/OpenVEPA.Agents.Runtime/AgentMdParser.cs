using System.Text;

using OpenVEPA.Core.Agents;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenVEPA.Agents.Runtime;

/// <summary>Parses agent definition files in the <c>&lt;name&gt;.agent.md</c> format.</summary>
public sealed class AgentMdParser
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>Parses an agent definition from the content of a <c>.agent.md</c> file.</summary>
    /// <param name="fileName">The file name, used as a fallback for the agent name.</param>
    /// <param name="content">The raw text content of the agent definition file.</param>
    /// <returns>A parsed <see cref="AgentDefinition"/>.</returns>
    public AgentDefinition Parse(string fileName, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Agent definition content is empty.", nameof(content));

        var (frontmatter, body) = SplitFrontmatter(content);
        var metadata = ParseFrontmatter(frontmatter);
        var sections = ParseMarkdownSections(body);

        var name = ExtractName(metadata, fileName);
        var description = metadata.GetValueOrDefault("description")?.ToString() ?? "";
        var skills = ParseStringList(metadata.GetValueOrDefault("openvepa-skills"));
        var autonomyLevel = ParseAutonomyLevel(metadata);
        var llmRequirements = ParseLlmRequirements(metadata.GetValueOrDefault("openvepa-llm-requirements"));
        var systemPrompt = BuildSystemPrompt(sections);

        return new AgentDefinition(
            Name: name,
            Description: description,
            SystemPrompt: systemPrompt,
            Skills: skills,
            AutonomyLevel: autonomyLevel,
            LlmRequirements: llmRequirements);
    }

    private static (string Frontmatter, string Body) SplitFrontmatter(string content)
    {
        const string delimiter = "---";
        var trimmed = content.TrimStart();

        if (!trimmed.StartsWith(delimiter, StringComparison.Ordinal))
            return ("", content);

        var endIndex = trimmed.IndexOf(delimiter, delimiter.Length, StringComparison.Ordinal);
        if (endIndex < 0)
            return ("", content);

        var frontmatter = trimmed[delimiter.Length..endIndex].Trim();
        var body = trimmed[(endIndex + delimiter.Length)..].TrimStart();

        return (frontmatter, body);
    }

    private static Dictionary<string, object?> ParseFrontmatter(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return new Dictionary<string, object?>();

        return YamlDeserializer.Deserialize<Dictionary<string, object?>>(yaml)
            ?? new Dictionary<string, object?>();
    }

    private static Dictionary<string, string> ParseMarkdownSections(string body)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;
        var currentContent = new StringBuilder();

        foreach (var line in body.Split('\n'))
        {
            if (line.TrimStart().StartsWith("## ", StringComparison.Ordinal))
            {
                if (currentSection is not null)
                    sections[currentSection] = currentContent.ToString().Trim();

                currentSection = line.TrimStart()[3..].Trim();
                currentContent.Clear();
            }
            else
            {
                currentContent.AppendLine(line);
            }
        }

        if (currentSection is not null)
            sections[currentSection] = currentContent.ToString().Trim();

        return sections;
    }

    private static string ExtractName(Dictionary<string, object?> metadata, string fileName)
    {
        if (metadata.TryGetValue("name", out var nameVal) && nameVal is string name && name.Length > 0)
            return name;

        return Path.GetFileNameWithoutExtension(fileName)
            .Replace(".agent", "", StringComparison.OrdinalIgnoreCase);
    }

    private static int ParseAutonomyLevel(Dictionary<string, object?> metadata)
    {
        if (!metadata.TryGetValue("openvepa-autonomy-default", out var value))
            return 0;

        return value switch
        {
            int i => i,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => Convert.ToInt32(value)
        };
    }

    private static IReadOnlyList<string> ParseStringList(object? value)
    {
        return value switch
        {
            IList<object> list => list
                .Select(item => item?.ToString() ?? "")
                .Where(s => s.Length > 0)
                .ToList(),
            string s => s
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            _ => []
        };
    }

    private static AgentLlmRequirements? ParseLlmRequirements(object? value)
    {
        if (value is not IDictionary<object, object> dict)
            return null;

        var capabilities = dict.TryGetValue("capabilities", out var caps)
            ? ParseStringList(caps)
            : (IReadOnlyList<string>)[];

        return new AgentLlmRequirements(capabilities);
    }

    private static string BuildSystemPrompt(Dictionary<string, string> sections)
    {
        var builder = new StringBuilder();

        if (sections.TryGetValue("System Prompt", out var systemPrompt))
            builder.AppendLine(systemPrompt);

        if (sections.TryGetValue("Guidance", out var guidance))
        {
            builder.AppendLine();
            builder.AppendLine("## Guidance");
            builder.AppendLine(guidance);
        }

        if (sections.TryGetValue("Hard Restrictions", out var restrictions))
        {
            builder.AppendLine();
            builder.AppendLine("## Hard Restrictions");
            builder.AppendLine(restrictions);
        }

        return builder.ToString().Trim();
    }
}
