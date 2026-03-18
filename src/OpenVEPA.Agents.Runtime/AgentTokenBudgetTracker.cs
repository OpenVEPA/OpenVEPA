using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OpenVEPA.Core.Agents;
using OpenVEPA.Storage;

namespace OpenVEPA.Agents.Runtime;

/// <summary>
/// Tracks and enforces token budgets for agents.
/// Uses in-memory counters for session-scoped budgets and the LLM audit log
/// for daily/monthly budgets.
/// </summary>
internal sealed class AgentTokenBudgetTracker : IAgentTokenBudgetTracker
{
    private readonly IDbContextFactory<OpenVepaDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AgentTokenBudgetTracker> _logger;

    /// <summary>In-memory session token counters keyed by agent name.</summary>
    private readonly ConcurrentDictionary<string, long> _sessionCounters = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Tracks the last LLM call timestamp per agent for pause-resume logic.</summary>
    private readonly ConcurrentDictionary<string, DateTime> _lastExceededAt = new(StringComparer.OrdinalIgnoreCase);

    public AgentTokenBudgetTracker(
        IDbContextFactory<OpenVepaDbContext> dbFactory,
        TimeProvider timeProvider,
        ILogger<AgentTokenBudgetTracker> logger)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BudgetCheckResult> CheckBudgetAsync(
        string agentName,
        AgentTokenBudget? budget,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new ArgumentException("Agent name is required.", nameof(agentName));
        }

        if (budget is null || budget.MaxTokensPerPeriod <= 0)
        {
            return new BudgetCheckResult(
                IsAllowed: true,
                IsPaused: false,
                Message: null,
                TokensUsed: 0,
                TokensRemaining: long.MaxValue,
                ResumeAfter: null);
        }

        long tokensUsed = await GetTokensUsedInPeriodAsync(agentName, budget.Period, ct).ConfigureAwait(false);
        long remaining = budget.MaxTokensPerPeriod - tokensUsed;

        if (remaining > 0)
        {
            return new BudgetCheckResult(
                IsAllowed: true,
                IsPaused: false,
                Message: null,
                TokensUsed: tokensUsed,
                TokensRemaining: remaining,
                ResumeAfter: null);
        }

        // Budget exceeded.
        return HandleExceeded(agentName, budget, tokensUsed);
    }

    public Task RecordUsageAsync(
        string agentName,
        int inputTokens,
        int outputTokens,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new ArgumentException("Agent name is required.", nameof(agentName));
        }

        long total = inputTokens + outputTokens;
        _sessionCounters.AddOrUpdate(agentName, total, (_, existing) => existing + total);

        _logger.LogDebug(
            "Recorded {Tokens} tokens for agent {AgentName} (session total: {Total})",
            total,
            agentName,
            _sessionCounters[agentName]);

        return Task.CompletedTask;
    }

    public async Task<AgentTokenUsageSummary> GetUsageSummaryAsync(
        string agentName,
        AgentTokenBudget? budget,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new ArgumentException("Agent name is required.", nameof(agentName));
        }

        BudgetPeriod period = budget?.Period ?? BudgetPeriod.Monthly;
        long maxTokens = budget?.MaxTokensPerPeriod ?? 0;
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        (DateTime periodStart, DateTime periodEnd) = GetPeriodBounds(period, now);

        if (period == BudgetPeriod.Session)
        {
            long sessionTotal = _sessionCounters.GetValueOrDefault(agentName, 0);
            return new AgentTokenUsageSummary(
                AgentName: agentName,
                TotalInputTokens: 0,
                TotalOutputTokens: 0,
                TotalTokens: sessionTotal,
                MaxTokensPerPeriod: maxTokens,
                Period: period,
                PeriodStart: periodStart,
                PeriodEnd: periodEnd);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var usage = await db.LlmAuditLogEntries
            .Where(e => e.AgentName == agentName && e.Timestamp >= periodStart && e.Timestamp < periodEnd)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                InputTokens = g.Sum(e => (long)e.InputTokens),
                OutputTokens = g.Sum(e => (long)e.OutputTokens)
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        long totalInput = usage?.InputTokens ?? 0;
        long totalOutput = usage?.OutputTokens ?? 0;

        return new AgentTokenUsageSummary(
            AgentName: agentName,
            TotalInputTokens: totalInput,
            TotalOutputTokens: totalOutput,
            TotalTokens: totalInput + totalOutput,
            MaxTokensPerPeriod: maxTokens,
            Period: period,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd);
    }

    private async Task<long> GetTokensUsedInPeriodAsync(
        string agentName,
        BudgetPeriod period,
        CancellationToken ct)
    {
        if (period == BudgetPeriod.Session)
        {
            return _sessionCounters.GetValueOrDefault(agentName, 0);
        }

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        (DateTime periodStart, DateTime periodEnd) = GetPeriodBounds(period, now);

        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        long total = await db.LlmAuditLogEntries
            .Where(e => e.AgentName == agentName && e.Timestamp >= periodStart && e.Timestamp < periodEnd)
            .SumAsync(e => (long)e.InputTokens + e.OutputTokens, ct)
            .ConfigureAwait(false);

        return total;
    }

    private BudgetCheckResult HandleExceeded(
        string agentName,
        AgentTokenBudget budget,
        long tokensUsed)
    {
        long remaining = budget.MaxTokensPerPeriod - tokensUsed;

        if (budget.ActionOnExceeded == BudgetAction.Stop)
        {
            _logger.LogWarning(
                "Agent {AgentName} exceeded token budget ({Used}/{Max}). Stopping.",
                agentName,
                tokensUsed,
                budget.MaxTokensPerPeriod);

            return new BudgetCheckResult(
                IsAllowed: false,
                IsPaused: false,
                Message: $"Token budget exceeded for agent '{agentName}'. Used {tokensUsed} of {budget.MaxTokensPerPeriod} tokens.",
                TokensUsed: tokensUsed,
                TokensRemaining: remaining,
                ResumeAfter: null);
        }

        // PauseResume: check if the pause duration has elapsed.
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        TimeSpan pauseDuration = TimeSpan.FromMinutes(budget.PauseResumeMinutes);

        if (_lastExceededAt.TryGetValue(agentName, out DateTime exceededAt))
        {
            TimeSpan elapsed = now - exceededAt;
            if (elapsed >= pauseDuration)
            {
                // Pause elapsed. Allow the call but re-mark the exceeded timestamp
                // so the next call that exceeds will pause again.
                _lastExceededAt[agentName] = now;

                _logger.LogInformation(
                    "Agent {AgentName} pause elapsed. Resuming despite budget exceeded ({Used}/{Max}).",
                    agentName,
                    tokensUsed,
                    budget.MaxTokensPerPeriod);

                return new BudgetCheckResult(
                    IsAllowed: true,
                    IsPaused: false,
                    Message: $"Agent '{agentName}' resumed after pause. Budget still exceeded ({tokensUsed}/{budget.MaxTokensPerPeriod}).",
                    TokensUsed: tokensUsed,
                    TokensRemaining: remaining,
                    ResumeAfter: null);
            }

            // Still paused.
            TimeSpan resumeAfter = pauseDuration - elapsed;

            _logger.LogInformation(
                "Agent {AgentName} is paused. Resumes in {Minutes:F1} minutes.",
                agentName,
                resumeAfter.TotalMinutes);

            return new BudgetCheckResult(
                IsAllowed: false,
                IsPaused: true,
                Message: $"Agent '{agentName}' is paused due to budget exceeded. Resumes in {resumeAfter.TotalMinutes:F1} minutes.",
                TokensUsed: tokensUsed,
                TokensRemaining: remaining,
                ResumeAfter: resumeAfter);
        }

        // First time exceeded with PauseResume. Record the exceeded time and pause.
        _lastExceededAt[agentName] = now;

        _logger.LogWarning(
            "Agent {AgentName} exceeded token budget ({Used}/{Max}). Pausing for {Minutes} minutes.",
            agentName,
            tokensUsed,
            budget.MaxTokensPerPeriod,
            budget.PauseResumeMinutes);

        return new BudgetCheckResult(
            IsAllowed: false,
            IsPaused: true,
            Message: $"Agent '{agentName}' exceeded budget. Paused for {budget.PauseResumeMinutes} minutes.",
            TokensUsed: tokensUsed,
            TokensRemaining: remaining,
            ResumeAfter: pauseDuration);
    }

    private static (DateTime Start, DateTime End) GetPeriodBounds(BudgetPeriod period, DateTime now)
    {
        return period switch
        {
            BudgetPeriod.Daily => (now.Date, now.Date.AddDays(1)),
            BudgetPeriod.Monthly => (
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)),
            BudgetPeriod.Session => (DateTime.MinValue, DateTime.MaxValue),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown budget period.")
        };
    }
}
