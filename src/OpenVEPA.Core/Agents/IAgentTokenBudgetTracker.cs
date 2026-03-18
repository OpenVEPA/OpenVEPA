namespace OpenVEPA.Core.Agents;

/// <summary>Tracks and enforces token budgets for agents.</summary>
public interface IAgentTokenBudgetTracker
{
    /// <summary>Checks whether the agent can proceed with an LLM call.</summary>
    /// <returns>A result indicating whether the call is allowed, paused, or blocked.</returns>
    Task<BudgetCheckResult> CheckBudgetAsync(
        string agentName,
        AgentTokenBudget? budget,
        CancellationToken ct = default);

    /// <summary>Records token usage for an agent after an LLM call completes.</summary>
    Task RecordUsageAsync(
        string agentName,
        int inputTokens,
        int outputTokens,
        CancellationToken ct = default);

    /// <summary>Gets the current token usage for an agent in the current budget period.</summary>
    Task<AgentTokenUsageSummary> GetUsageSummaryAsync(
        string agentName,
        AgentTokenBudget? budget,
        CancellationToken ct = default);
}
