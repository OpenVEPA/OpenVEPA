using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Preferences;

namespace OpenVEPA.Agents.Runtime;

/// <summary>
/// Default implementation of <see cref="IAgentRuntime"/> that loads agent definitions,
/// manages session context, and routes messages to the appropriate agent.
/// </summary>
public sealed class AgentRuntime : IAgentRuntime
{
    private readonly AssistantAgent _assistant;
    private readonly AgentDirectory _agentDirectory;
    private readonly IUserProfileService _userProfileService;
    private readonly ILogger<AgentRuntime> _logger;
    private readonly AgentOptions _options;

    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessionHistory = new();
    private IReadOnlyList<AgentDefinition>? _agents;

    /// <summary>Initializes a new instance of the <see cref="AgentRuntime"/> class.</summary>
    public AgentRuntime(
        AssistantAgent assistant,
        AgentDirectory agentDirectory,
        IUserProfileService userProfileService,
        IOptions<AgentOptions> options,
        ILogger<AgentRuntime> logger)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        _assistant = assistant ?? throw new ArgumentNullException(nameof(assistant));
        _agentDirectory = agentDirectory ?? throw new ArgumentNullException(nameof(agentDirectory));
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AgentResponse> ProcessMessageAsync(
        string sessionId,
        string message,
        CancellationToken ct)
    {
        ValidateInputs(sessionId, message);

        var agent = GetDefaultAgent();
        var history = GetSessionHistory(sessionId);
        var preferences = await LoadPreferencesAsync(ct);

        var response = await _assistant.ProcessAsync(agent, history, message, preferences, ct);

        AppendToHistory(sessionId, ChatRole.User, message);
        AppendToHistory(sessionId, ChatRole.Assistant, response.Content);

        return response;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamResponseAsync(
        string sessionId,
        string message,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ValidateInputs(sessionId, message);

        var agent = GetDefaultAgent();
        var history = GetSessionHistory(sessionId);
        var preferences = await LoadPreferencesAsync(ct);

        var fullResponse = new StringBuilder();

        await foreach (var token in _assistant.StreamAsync(agent, history, message, preferences, ct))
        {
            fullResponse.Append(token);
            yield return token;
        }

        AppendToHistory(sessionId, ChatRole.User, message);
        AppendToHistory(sessionId, ChatRole.Assistant, fullResponse.ToString());
    }

    private AgentDefinition GetDefaultAgent()
    {
        _agents ??= _agentDirectory.DiscoverAgents();

        var defaultAgent = _agents.FirstOrDefault(a =>
            string.Equals(a.Name, _options.DefaultAgent, StringComparison.OrdinalIgnoreCase));

        if (defaultAgent is not null)
            return defaultAgent;

        _logger.LogWarning(
            "Default agent '{DefaultAgent}' not found among {Count} discovered agent(s). Using built-in fallback.",
            _options.DefaultAgent, _agents.Count);

        return new AgentDefinition(
            Name: "assistant",
            Description: "General-purpose assistant",
            SystemPrompt: "You are a helpful assistant. Answer questions clearly and concisely.",
            Skills: [],
            AutonomyLevel: 0,
            LlmRequirements: null);
    }

    private IReadOnlyList<ChatMessage> GetSessionHistory(string sessionId)
    {
        return _sessionHistory.TryGetValue(sessionId, out var history)
            ? history.AsReadOnly()
            : [];
    }

    private void AppendToHistory(string sessionId, ChatRole role, string content)
    {
        var history = _sessionHistory.GetOrAdd(sessionId, _ => []);
        history.Add(new ChatMessage(role, content));
    }

    private async Task<UserPreferences?> LoadPreferencesAsync(CancellationToken ct)
    {
        try
        {
            return await _userProfileService.GetRelevantPreferencesAsync(null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load user preferences. Proceeding without them.");
            return null;
        }
    }

    private static void ValidateInputs(string sessionId, string message)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));
    }
}
