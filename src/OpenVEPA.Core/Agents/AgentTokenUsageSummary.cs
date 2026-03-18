namespace OpenVEPA.Core.Agents;

/// <summary>Token usage summary for an agent in the current budget period.</summary>
public sealed record AgentTokenUsageSummary(
    string AgentName,
    long TotalInputTokens,
    long TotalOutputTokens,
    long TotalTokens,
    long MaxTokensPerPeriod,
    BudgetPeriod Period,
    DateTime PeriodStart,
    DateTime PeriodEnd);
