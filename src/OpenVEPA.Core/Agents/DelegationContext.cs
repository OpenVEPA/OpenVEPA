namespace OpenVEPA.Core.Agents;

/// <summary>Context passed to a sub-agent during delegation.</summary>
public sealed record DelegationContext(
    /// <summary>The original user message that triggered delegation.</summary>
    string OriginalMessage,
    /// <summary>Extracted relevant messages from conversation history (not the full history).</summary>
    IReadOnlyList<string> RelevantHistory,
    /// <summary>Summary of user preferences, if available.</summary>
    string? UserPreferencesSummary,
    /// <summary>The orchestrator's description of what the sub-agent should do.</summary>
    string? TaskDescription,
    /// <summary>Remaining token budget available for this delegation.</summary>
    AgentTokenBudget? BudgetRemaining);
