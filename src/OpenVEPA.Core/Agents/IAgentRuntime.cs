namespace OpenVEPA.Core.Agents;

/// <summary>Processes messages and streams responses on behalf of an agent.</summary>
public interface IAgentRuntime
{
    /// <summary>Processes a user message and returns a complete response.</summary>
    Task<AgentResponse> ProcessMessageAsync(string sessionId, string message, CancellationToken ct);

    /// <summary>Streams response tokens for a user message.</summary>
    IAsyncEnumerable<string> StreamResponseAsync(string sessionId, string message, CancellationToken ct);

    /// <summary>Delegates a task to a named sub-agent and returns the result.</summary>
    Task<DelegationResult> DelegateToAgentAsync(
        string agentName,
        DelegationContext context,
        CancellationToken ct = default);
}
