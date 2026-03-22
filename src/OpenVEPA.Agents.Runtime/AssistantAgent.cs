using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Preferences;
using OpenVEPA.Core.Skills;

namespace OpenVEPA.Agents.Runtime;

/// <summary>
/// Core assistant agent that processes user messages through an LLM with skill-based tool calling.
/// Constructs prompts, invokes tools on behalf of the LLM, and returns final responses.
/// </summary>
public sealed class AssistantAgent
{
    private const int MaxToolCallIterations = 10;

    private readonly IAgentChatClientFactory _chatClientFactory;
    private readonly ISkillRuntime _skillRuntime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AssistantAgent> _logger;

    /// <summary>Initializes a new instance of the <see cref="AssistantAgent"/> class.</summary>
    public AssistantAgent(
        IAgentChatClientFactory chatClientFactory,
        ISkillRuntime skillRuntime,
        IConfiguration configuration,
        ILogger<AssistantAgent> logger)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _skillRuntime = skillRuntime ?? throw new ArgumentNullException(nameof(skillRuntime));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Processes a user message and returns a complete response with tool results.</summary>
    /// <param name="agent">The agent definition governing behavior.</param>
    /// <param name="history">Prior conversation messages.</param>
    /// <param name="userMessage">The current user message.</param>
    /// <param name="preferences">Optional user preferences for personalization.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A complete agent response.</returns>
    public async Task<AgentResponse> ProcessAsync(
        AgentDefinition agent,
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        UserPreferences? preferences,
        CancellationToken ct)
    {
        if (agent is null) throw new ArgumentNullException(nameof(agent));
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message is required.", nameof(userMessage));

        var messages = BuildMessageList(agent, history, userMessage, preferences);
        var chatClient = _chatClientFactory.GetChatClient(agent);
        var tools = BuildToolList(agent, chatClient, preferences);
        var options = CreateChatOptions(tools);
        var skillResults = new List<SkillResult>();

        _logger.LogInformation(
            "Sending LLM request: agent={Agent}, model={Model}, messages={Count}",
            agent.Name, agent.LlmConfig?.Model ?? "(default)", messages.Count);

        for (int iteration = 0; iteration < MaxToolCallIterations; iteration++)
        {
            _logger.LogDebug(
                "LLM call iteration {Iteration} for agent {Agent}",
                iteration, agent.Name);

            var response = await chatClient.GetResponseAsync(messages, options, ct);
            var functionCalls = ExtractFunctionCalls(response);

            if (functionCalls.Count == 0)
            {
                return new AgentResponse(
                    response.Text ?? "",
                    skillResults.Count > 0 ? skillResults : null,
                    MapTokenUsage(response.Usage));
            }

            AppendResponseMessages(messages, response);
            await ExecuteToolCallsAsync(
                functionCalls, messages, skillResults, chatClient, preferences, agent.Permissions, ct);
        }

        _logger.LogWarning(
            "Max tool call iterations ({Max}) reached for agent {Agent}",
            MaxToolCallIterations, agent.Name);

        return new AgentResponse(
            "I was unable to complete the request within the allowed number of tool calls.",
            skillResults.Count > 0 ? skillResults : null,
            null);
    }

    /// <summary>Streams response tokens for a user message.</summary>
    /// <param name="agent">The agent definition governing behavior.</param>
    /// <param name="history">Prior conversation messages.</param>
    /// <param name="userMessage">The current user message.</param>
    /// <param name="preferences">Optional user preferences for personalization.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of response text tokens.</returns>
    public async IAsyncEnumerable<string> StreamAsync(
        AgentDefinition agent,
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        UserPreferences? preferences,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (agent is null) throw new ArgumentNullException(nameof(agent));

        var chatClient = _chatClientFactory.GetChatClient(agent);
        var tools = BuildToolList(agent, chatClient, preferences);

        // Phase 1: fall back to non-streaming when tools are configured.
        // This avoids the complexity of handling mid-stream tool calls.
        if (tools.Count > 0)
        {
            var response = await ProcessAsync(agent, history, userMessage, preferences, ct);
            yield return response.Content;
            yield break;
        }

        var messages = BuildMessageList(agent, history, userMessage, preferences);

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options: null, ct))
        {
            if (update.Text is { Length: > 0 } text)
                yield return text;
        }
    }

    private static List<ChatMessage> BuildMessageList(
        AgentDefinition agent,
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        UserPreferences? preferences)
    {
        var messages = new List<ChatMessage>(history.Count + 2);

        var systemPrompt = BuildSystemPrompt(agent, preferences);
        messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
        messages.AddRange(history);
        messages.Add(new ChatMessage(ChatRole.User, userMessage));

        return messages;
    }

    private static string BuildSystemPrompt(AgentDefinition agent, UserPreferences? preferences)
    {
        var builder = new StringBuilder();
        builder.AppendLine(agent.SystemPrompt);

        if (preferences is { Entries.Count: > 0 })
        {
            builder.AppendLine();
            builder.AppendLine("## User Preferences");
            foreach (var (key, entry) in preferences.Entries)
                builder.AppendLine($"- {key}: {entry.Value}");
        }

        return builder.ToString().TrimEnd();
    }

    private List<AITool> BuildToolList(AgentDefinition agent, IChatClient chatClient, UserPreferences? preferences)
    {
        var allSkills = _skillRuntime.ListSkills();

        var relevantSkills = agent.Skills.Count > 0
            ? allSkills.Where(s => agent.Skills.Contains(s.Name, StringComparer.OrdinalIgnoreCase)).ToList()
            : allSkills.ToList();

        if (relevantSkills.Count == 0)
            return [];

        var executionContext = new SkillExecutionContext(
            chatClient,
            _logger,
            _configuration,
            preferences,
            CancellationToken.None,
            agent.Permissions);

        return relevantSkills
            .Select(manifest => (AITool)new SkillAIFunction(manifest, _skillRuntime, executionContext))
            .ToList();
    }

    private static ChatOptions? CreateChatOptions(List<AITool> tools)
    {
        if (tools.Count == 0)
            return null;

        return new ChatOptions { Tools = tools };
    }

    private static List<FunctionCallContent> ExtractFunctionCalls(ChatResponse response)
    {
        return response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionCallContent>()
            .ToList();
    }

    private static void AppendResponseMessages(
        List<ChatMessage> messages,
        ChatResponse response)
    {
        foreach (var message in response.Messages)
            messages.Add(message);
    }

    private async Task ExecuteToolCallsAsync(
        List<FunctionCallContent> functionCalls,
        List<ChatMessage> messages,
        List<SkillResult> skillResults,
        IChatClient chatClient,
        UserPreferences? preferences,
        AgentPermissions? permissions,
        CancellationToken ct)
    {
        foreach (var call in functionCalls)
        {
            _logger.LogDebug(
                "Executing tool call: {Name} (CallId: {CallId})",
                call.Name, call.CallId);

            var result = await InvokeSkillAsync(call, chatClient, preferences, permissions, ct);
            skillResults.Add(result);

            var resultJson = FormatSkillResult(result);
            messages.Add(new ChatMessage(
                ChatRole.Tool,
                [new FunctionResultContent(call.CallId, resultJson)]));
        }
    }

    private async Task<SkillResult> InvokeSkillAsync(
        FunctionCallContent call,
        IChatClient chatClient,
        UserPreferences? preferences,
        AgentPermissions? permissions,
        CancellationToken ct)
    {
        var executionContext = new SkillExecutionContext(
            chatClient,
            _logger,
            _configuration,
            preferences,
            ct,
            permissions);

        var parameters = call.Arguments?.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value)
            ?? new Dictionary<string, object?>();

        var prompt = parameters.TryGetValue("prompt", out var promptVal)
            ? promptVal?.ToString() ?? ""
            : "";

        var input = new SkillInput(prompt, parameters);

        try
        {
            return await _skillRuntime.ExecuteAsync(call.Name, input, executionContext, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skill execution failed: {Name}", call.Name);
            return new SkillResult(
                Success: false,
                Data: null,
                Error: new SkillError(SkillErrorKind.Permanent, ex.Message, ex),
                TokenUsage: null);
        }
    }

    private static string FormatSkillResult(SkillResult result)
    {
        if (!result.Success)
        {
            return JsonSerializer.Serialize(
                new { error = result.Error?.Message ?? "Unknown error" });
        }

        return result.Data is not null
            ? JsonSerializer.Serialize(result.Data)
            : JsonSerializer.Serialize(new { status = "success" });
    }

    private static TokenUsage? MapTokenUsage(UsageDetails? usage)
    {
        if (usage is null)
            return null;

        return new TokenUsage(
            InputTokens: (int)(usage.InputTokenCount ?? 0),
            OutputTokens: (int)(usage.OutputTokenCount ?? 0),
            EstimatedCostUsd: null);
    }
}
