using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenVEPA.Core.Agents;

namespace OpenVEPA.Agents.Runtime;

/// <summary>Discovers agent definitions from the configured agents directory.</summary>
public sealed class AgentDirectory
{
    private readonly AgentMdParser _parser;
    private readonly AgentOptions _options;
    private readonly ILogger<AgentDirectory> _logger;

    /// <summary>Initializes a new instance of the <see cref="AgentDirectory"/> class.</summary>
    public AgentDirectory(
        AgentMdParser parser,
        IOptions<AgentOptions> options,
        ILogger<AgentDirectory> logger)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>The built-in assistant agent that is always present.</summary>
    internal static readonly AgentDefinition BuiltInAssistant = new(
        Name: "assistant",
        Description: "Your personal AI assistant \u2014 the core brain that orchestrates conversations, delegates to skills, and learns your preferences.",
        SystemPrompt: "You are a helpful, versatile AI assistant. Answer questions clearly and concisely. "
            + "When the user has preferences configured, respect their communication style. "
            + "You can delegate work to specialized skills when appropriate.",
        Skills: [],
        AutonomyLevel: 2,
        LlmRequirements: null,
        IsSystem: true);

    /// <summary>Scans the agents directory and returns all discovered agent definitions, always including the assistant first.</summary>
    /// <remarks>
    /// If an <c>assistant.agent.md</c> file is found on disk, it overrides the hardcoded
    /// <see cref="BuiltInAssistant"/> fallback, allowing the orchestrator prompt and settings
    /// to be customized without recompilation.
    /// </remarks>
    public IReadOnlyList<AgentDefinition> DiscoverAgents()
    {
        var agentsPath = Path.GetFullPath(_options.AgentsDirectory);

        if (!Directory.Exists(agentsPath))
        {
            _logger.LogWarning("Agents directory not found: {Path}", agentsPath);
            return [BuiltInAssistant];
        }

        AgentDefinition assistant = BuiltInAssistant;
        var agents = new List<AgentDefinition>();

        foreach (var subdirectory in Directory.EnumerateDirectories(agentsPath))
        {
            var definition = TryLoadAgent(subdirectory);
            if (definition is null)
                continue;

            if (string.Equals(definition.Name, BuiltInAssistant.Name, StringComparison.OrdinalIgnoreCase))
            {
                assistant = definition with { IsSystem = true };
                _logger.LogInformation("Loaded assistant definition from file, overriding built-in fallback");
            }
            else
            {
                agents.Add(definition);
            }
        }

        agents.Insert(0, assistant);

        _logger.LogInformation("Discovered {Count} agent(s) in {Path} (including assistant)", agents.Count, agentsPath);
        return agents;
    }

    private AgentDefinition? TryLoadAgent(string subdirectory)
    {
        var agentName = Path.GetFileName(subdirectory);
        var agentFile = Path.Combine(subdirectory, $"{agentName}.agent.md");

        if (!File.Exists(agentFile))
            return null;

        try
        {
            var content = File.ReadAllText(agentFile);
            var definition = _parser.Parse(agentFile, content);
            _logger.LogDebug("Discovered agent: {Name}", definition.Name);
            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse agent definition: {File}", agentFile);
            return null;
        }
    }
}
