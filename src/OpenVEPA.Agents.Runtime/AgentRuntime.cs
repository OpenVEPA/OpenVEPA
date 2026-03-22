using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Preferences;

namespace OpenVEPA.Agents.Runtime;

/// <summary>
/// Default implementation of <see cref="IAgentRuntime"/> that loads agent definitions,
/// manages session context, classifies intent via trigger matching, and routes
/// messages to the appropriate specialist agent or the default orchestrator.
/// </summary>
public sealed class AgentRuntime : IAgentRuntime
{
    private readonly AssistantAgent _assistant;
    private readonly AgentDirectory _agentDirectory;
    private readonly IUserProfileService _userProfileService;
    private readonly ISystemPromptBuilder _systemPromptBuilder;
    private readonly IAgentTokenBudgetTracker _budgetTracker;
    private readonly ILogger<AgentRuntime> _logger;
    private readonly AgentOptions _options;

    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessionHistory = new();
    private IReadOnlyList<AgentDefinition>? _agents;

    private static readonly JsonSerializerOptions s_configJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>Initializes a new instance of the <see cref="AgentRuntime"/> class.</summary>
    public AgentRuntime(
        AssistantAgent assistant,
        AgentDirectory agentDirectory,
        IUserProfileService userProfileService,
        ISystemPromptBuilder systemPromptBuilder,
        IAgentTokenBudgetTracker budgetTracker,
        IOptions<AgentOptions> options,
        ILogger<AgentRuntime> logger)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        _assistant = assistant ?? throw new ArgumentNullException(nameof(assistant));
        _agentDirectory = agentDirectory ?? throw new ArgumentNullException(nameof(agentDirectory));
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        _systemPromptBuilder = systemPromptBuilder ?? throw new ArgumentNullException(nameof(systemPromptBuilder));
        _budgetTracker = budgetTracker ?? throw new ArgumentNullException(nameof(budgetTracker));
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

        var agents = DiscoverAgents();
        var orchestrator = GetOrchestratorAgent(agents);
        var preferences = await LoadPreferencesAsync(ct).ConfigureAwait(false);
        orchestrator = ApplySavedConfig(orchestrator, preferences);

        // Enforce the orchestrator's token budget before any LLM call.
        var budgetCheck = await _budgetTracker.CheckBudgetAsync(
            orchestrator.Name, orchestrator.TokenBudget, ct).ConfigureAwait(false);

        if (!budgetCheck.IsAllowed)
        {
            return new AgentResponse(
                budgetCheck.Message ?? "Token budget exceeded.",
                null, null);
        }

        // Phase 1 — trigger-based delegation to specialist agents.
        var matchedAgent = MatchTriggers(message, agents, orchestrator);
        if (matchedAgent is not null)
        {
            _logger.LogInformation(
                "Trigger matched specialist '{Agent}' for message in session {Session}.",
                matchedAgent.Name, sessionId);

            var delegationResult = await DelegateWithBudgetAsync(
                matchedAgent, message, preferences, ct).ConfigureAwait(false);

            AppendToHistory(sessionId, ChatRole.User, message);
            AppendToHistory(sessionId, ChatRole.Assistant, delegationResult.Response);

            return FormatDelegationResponse(delegationResult);
        }

        // No trigger match — orchestrator handles directly with a dynamic system prompt.
        var history = GetSessionHistory(sessionId);
        var promptContext = new SystemPromptContext(
            orchestrator, preferences, agents,
            _options.DelegationVisibility, budgetCheck);
        var dynamicPrompt = _systemPromptBuilder.Build(promptContext);
        var augmentedAgent = orchestrator with { SystemPrompt = dynamicPrompt };

        var response = await _assistant.ProcessAsync(
            augmentedAgent, history, message, preferences, ct).ConfigureAwait(false);

        // Record token usage against the orchestrator's budget.
        if (response.TotalTokenUsage is { } usage)
        {
            await _budgetTracker.RecordUsageAsync(
                orchestrator.Name, usage.InputTokens, usage.OutputTokens, ct).ConfigureAwait(false);
        }

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

        var agents = DiscoverAgents();
        var orchestrator = GetOrchestratorAgent(agents);
        var preferences = await LoadPreferencesAsync(ct).ConfigureAwait(false);
        orchestrator = ApplySavedConfig(orchestrator, preferences);

        // Enforce the orchestrator's token budget.
        var budgetCheck = await _budgetTracker.CheckBudgetAsync(
            orchestrator.Name, orchestrator.TokenBudget, ct).ConfigureAwait(false);

        if (!budgetCheck.IsAllowed)
        {
            yield return budgetCheck.Message ?? "Token budget exceeded.";
            yield break;
        }

        // Phase 1 — trigger-based delegation to specialist agents.
        var matchedAgent = MatchTriggers(message, agents, orchestrator);
        if (matchedAgent is not null)
        {
            _logger.LogInformation(
                "Trigger matched specialist '{Agent}' for streaming in session {Session}.",
                matchedAgent.Name, sessionId);

            var delegationResult = await DelegateWithBudgetAsync(
                matchedAgent, message, preferences, ct).ConfigureAwait(false);

            AppendToHistory(sessionId, ChatRole.User, message);
            AppendToHistory(sessionId, ChatRole.Assistant, delegationResult.Response);

            var formatted = FormatDelegationResponse(delegationResult);
            yield return formatted.Content;
            yield break;
        }

        // No trigger match — orchestrator handles with dynamic prompt.
        var history = GetSessionHistory(sessionId);
        var promptContext = new SystemPromptContext(
            orchestrator, preferences, agents,
            _options.DelegationVisibility, budgetCheck);
        var dynamicPrompt = _systemPromptBuilder.Build(promptContext);
        var augmentedAgent = orchestrator with { SystemPrompt = dynamicPrompt };

        var fullResponse = new StringBuilder();

        await foreach (var token in _assistant.StreamAsync(augmentedAgent, history, message, preferences, ct))
        {
            fullResponse.Append(token);
            yield return token;
        }

        AppendToHistory(sessionId, ChatRole.User, message);
        AppendToHistory(sessionId, ChatRole.Assistant, fullResponse.ToString());
    }

    /// <inheritdoc />
    public async Task<DelegationResult> DelegateToAgentAsync(
        string agentName,
        DelegationContext context,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(context);

        var agents = DiscoverAgents();

        var targetAgent = agents.FirstOrDefault(a =>
            string.Equals(a.Name, agentName, StringComparison.OrdinalIgnoreCase));

        if (targetAgent is null)
        {
            _logger.LogWarning("Delegation target agent '{AgentName}' not found.", agentName);
            return new DelegationResult(
                AgentName: agentName,
                Response: string.Empty,
                TokensUsed: null,
                Success: false,
                ErrorMessage: $"Agent '{agentName}' not found.");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var preferences = await LoadPreferencesAsync(ct).ConfigureAwait(false);
            targetAgent = ApplySavedConfig(targetAgent, preferences);

            // Build a dynamic system prompt for the target agent.
            var promptContext = new SystemPromptContext(targetAgent, preferences);
            var dynamicPrompt = _systemPromptBuilder.Build(promptContext);
            var augmentedAgent = targetAgent with { SystemPrompt = dynamicPrompt };

            var response = await _assistant.ProcessAsync(
                augmentedAgent, [], context.OriginalMessage, preferences, ct).ConfigureAwait(false);

            stopwatch.Stop();

            return new DelegationResult(
                AgentName: agentName,
                Response: response.Content,
                TokensUsed: response.TotalTokenUsage,
                Success: true,
                Duration: stopwatch.Elapsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Delegation to agent '{AgentName}' failed.", agentName);

            return new DelegationResult(
                AgentName: agentName,
                Response: string.Empty,
                TokensUsed: null,
                Success: false,
                ErrorMessage: ex.Message,
                Duration: stopwatch.Elapsed);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────

    private IReadOnlyList<AgentDefinition> DiscoverAgents()
    {
        _agents ??= _agentDirectory.DiscoverAgents();
        return _agents;
    }

    private AgentDefinition GetOrchestratorAgent(IReadOnlyList<AgentDefinition> agents)
    {
        var defaultAgent = agents.FirstOrDefault(a =>
            string.Equals(a.Name, _options.DefaultAgent, StringComparison.OrdinalIgnoreCase));

        if (defaultAgent is not null)
            return defaultAgent;

        _logger.LogWarning(
            "Default agent '{DefaultAgent}' not found among {Count} discovered agent(s). Using built-in fallback.",
            _options.DefaultAgent, agents.Count);

        return new AgentDefinition(
            Name: "assistant",
            Description: "General-purpose assistant",
            SystemPrompt: "You are a helpful assistant. Answer questions clearly and concisely.",
            Skills: [],
            AutonomyLevel: 0,
            LlmRequirements: null,
            IsSystem: true);
    }

    private AgentDefinition? MatchTriggers(
        string message, IReadOnlyList<AgentDefinition> agents, AgentDefinition orchestrator)
    {
        var lowerMessage = message.ToLowerInvariant();

        return agents
            .Where(a => a.Name != orchestrator.Name && a.Triggers is { Count: > 0 })
            .Where(a => a.Triggers!.Any(t => lowerMessage.Contains(t.ToLowerInvariant())))
            .OrderByDescending(a => a.Priority)
            .FirstOrDefault();
    }

    private async Task<DelegationResult> DelegateWithBudgetAsync(
        AgentDefinition target, string message, UserPreferences? preferences, CancellationToken ct)
    {
        var budgetCheck = await _budgetTracker.CheckBudgetAsync(
            target.Name, target.TokenBudget, ct).ConfigureAwait(false);

        if (!budgetCheck.IsAllowed)
        {
            return new DelegationResult(
                target.Name, budgetCheck.Message ?? "Budget exceeded",
                null, false, budgetCheck.Message);
        }

        var context = new DelegationContext(
            OriginalMessage: message,
            RelevantHistory: [],
            UserPreferencesSummary: preferences?.Entries.Count > 0
                ? string.Join("; ", preferences.Entries.Select(e => $"{e.Key}: {e.Value.Value}"))
                : null,
            TaskDescription: null,
            BudgetRemaining: target.TokenBudget);

        var result = await DelegateToAgentAsync(target.Name, context, ct).ConfigureAwait(false);

        if (result.TokensUsed is { } usage)
        {
            await _budgetTracker.RecordUsageAsync(
                target.Name, usage.InputTokens, usage.OutputTokens, ct).ConfigureAwait(false);
        }

        return result;
    }

    private AgentResponse FormatDelegationResponse(DelegationResult result)
    {
        if (!result.Success)
        {
            return new AgentResponse(
                $"I encountered an issue: {result.ErrorMessage}",
                null, result.TokensUsed);
        }

        var content = _options.DelegationVisibility switch
        {
            DelegationVisibility.Invisible => result.Response,
            DelegationVisibility.Visible => $"*Consulted {result.AgentName}*\n\n{result.Response}",
            DelegationVisibility.Detailed =>
                $"**Delegated to: {result.AgentName}** " +
                $"(Duration: {result.Duration.TotalSeconds:F1}s, " +
                $"Tokens: {result.TokensUsed?.InputTokens + result.TokensUsed?.OutputTokens})\n\n" +
                result.Response,
            _ => result.Response
        };

        return new AgentResponse(content, null, result.TokensUsed);
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
            return await _userProfileService.GetRelevantPreferencesAsync(null, ct).ConfigureAwait(false);
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

    /// <summary>
    /// Applies user-saved agent configuration (from preferences) over the base agent definition.
    /// </summary>
    private static AgentDefinition ApplySavedConfig(AgentDefinition agent, UserPreferences? preferences)
    {
        if (preferences is null)
            return agent;

        var key = $"agent-config:{agent.Name}";
        if (!preferences.Entries.TryGetValue(key, out var entry) || string.IsNullOrWhiteSpace(entry.Value))
            return agent;

        try
        {
            var saved = JsonSerializer.Deserialize<SavedAgentConfig>(entry.Value, s_configJsonOptions);
            if (saved is null)
                return agent;

            return agent with
            {
                LlmConfig = saved.LlmConfig ?? agent.LlmConfig,
                Permissions = saved.Permissions ?? agent.Permissions,
                TokenBudget = saved.TokenBudget ?? agent.TokenBudget,
                Triggers = saved.Triggers ?? agent.Triggers,
                Restrictions = saved.Restrictions ?? agent.Restrictions,
                Priority = saved.Priority ?? agent.Priority
            };
        }
        catch (Exception)
        {
            // Deserialization failure should not break the runtime.
            return agent;
        }
    }

    private sealed record SavedAgentConfig(
        AgentLlmConfig? LlmConfig,
        AgentPermissions? Permissions,
        AgentTokenBudget? TokenBudget,
        IReadOnlyList<string>? Triggers,
        IReadOnlyList<string>? Restrictions,
        int? Priority);
}
