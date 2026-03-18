namespace OpenVEPA.Core.Agents;

/// <summary>Token budget configuration for an agent.</summary>
public sealed record AgentTokenBudget(
    /// <summary>Maximum tokens allowed per period. 0 means unlimited.</summary>
    long MaxTokensPerPeriod = 0,
    /// <summary>The time period over which the budget is measured.</summary>
    BudgetPeriod Period = BudgetPeriod.Monthly,
    /// <summary>Action to take when the budget is exceeded.</summary>
    BudgetAction ActionOnExceeded = BudgetAction.Stop,
    /// <summary>Minutes to pause before resuming when <see cref="BudgetAction.PauseResume"/> is used.</summary>
    int PauseResumeMinutes = 60);

/// <summary>Time period over which an agent's token budget is measured.</summary>
public enum BudgetPeriod
{
    /// <summary>Budget resets each session.</summary>
    Session,

    /// <summary>Budget resets daily.</summary>
    Daily,

    /// <summary>Budget resets monthly.</summary>
    Monthly
}

/// <summary>Action to take when an agent exceeds its token budget.</summary>
public enum BudgetAction
{
    /// <summary>Stop the agent immediately.</summary>
    Stop,

    /// <summary>Pause the agent and resume after a configured delay.</summary>
    PauseResume
}
