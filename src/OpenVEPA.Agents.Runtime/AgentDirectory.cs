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

    /// <summary>Scans the agents directory and returns all discovered agent definitions.</summary>
    public IReadOnlyList<AgentDefinition> DiscoverAgents()
    {
        var agentsPath = Path.GetFullPath(_options.AgentsDirectory);

        if (!Directory.Exists(agentsPath))
        {
            _logger.LogWarning("Agents directory not found: {Path}", agentsPath);
            return [];
        }

        var agents = new List<AgentDefinition>();

        foreach (var subdirectory in Directory.EnumerateDirectories(agentsPath))
        {
            var definition = TryLoadAgent(subdirectory);
            if (definition is not null)
                agents.Add(definition);
        }

        _logger.LogInformation("Discovered {Count} agent(s) in {Path}", agents.Count, agentsPath);
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
