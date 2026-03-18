namespace OpenVEPA.Core.Agents;

/// <summary>Result of a budget check before making an LLM call.</summary>
public sealed record BudgetCheckResult(
    bool IsAllowed,
    bool IsPaused,
    string? Message,
    long TokensUsed,
    long TokensRemaining,
    TimeSpan? ResumeAfter);
