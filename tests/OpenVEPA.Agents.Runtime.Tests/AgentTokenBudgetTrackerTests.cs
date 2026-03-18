using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using OpenVEPA.Agents.Runtime;
using OpenVEPA.Core.Agents;
using OpenVEPA.Storage;
using OpenVEPA.Storage.Entities;

namespace OpenVEPA.Agents.Runtime.Tests;

public sealed class AgentTokenBudgetTrackerTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _serviceProvider = null!;
    private IDbContextFactory<OpenVepaDbContext> _dbFactory = null!;
    private FakeTimeProvider _timeProvider = null!;
    private AgentTokenBudgetTracker _tracker = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<OpenVepaDbContext>(options =>
        {
            options.UseSqlite(_connection);
        });

        _serviceProvider = services.BuildServiceProvider();
        _dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<OpenVepaDbContext>>();

        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));
        _tracker = new AgentTokenBudgetTracker(
            _dbFactory,
            _timeProvider,
            NullLogger<AgentTokenBudgetTracker>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task CheckBudget_NullBudget_AlwaysAllowed()
    {
        var result = await _tracker.CheckBudgetAsync("test-agent", budget: null);

        result.IsAllowed.Should().BeTrue();
        result.IsPaused.Should().BeFalse();
        result.TokensRemaining.Should().Be(long.MaxValue);
    }

    [Fact]
    public async Task CheckBudget_UnlimitedBudget_AlwaysAllowed()
    {
        var budget = new AgentTokenBudget(MaxTokensPerPeriod: 0);

        var result = await _tracker.CheckBudgetAsync("test-agent", budget);

        result.IsAllowed.Should().BeTrue();
        result.IsPaused.Should().BeFalse();
    }

    [Fact]
    public async Task CheckBudget_DailyPeriodUnderBudget_Allowed()
    {
        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 10_000,
            Period: BudgetPeriod.Daily);

        await SeedAuditLogAsync("daily-agent", inputTokens: 2000, outputTokens: 1000,
            timestamp: _timeProvider.GetUtcNow().UtcDateTime);

        var result = await _tracker.CheckBudgetAsync("daily-agent", budget);

        result.IsAllowed.Should().BeTrue();
        result.IsPaused.Should().BeFalse();
        result.TokensUsed.Should().Be(3000);
        result.TokensRemaining.Should().Be(7000);
    }

    [Fact]
    public async Task CheckBudget_DailyPeriodOverBudgetWithStop_Blocked()
    {
        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 5000,
            Period: BudgetPeriod.Daily,
            ActionOnExceeded: BudgetAction.Stop);

        await SeedAuditLogAsync("stop-agent", inputTokens: 3000, outputTokens: 3000,
            timestamp: _timeProvider.GetUtcNow().UtcDateTime);

        var result = await _tracker.CheckBudgetAsync("stop-agent", budget);

        result.IsAllowed.Should().BeFalse();
        result.IsPaused.Should().BeFalse();
        result.TokensUsed.Should().Be(6000);
        result.TokensRemaining.Should().Be(-1000);
        result.Message.Should().Contain("exceeded");
    }

    [Fact]
    public async Task CheckBudget_OverBudgetWithPauseResume_PausedNotElapsed()
    {
        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 5000,
            Period: BudgetPeriod.Daily,
            ActionOnExceeded: BudgetAction.PauseResume,
            PauseResumeMinutes: 30);

        await SeedAuditLogAsync("pause-agent", inputTokens: 3000, outputTokens: 3000,
            timestamp: _timeProvider.GetUtcNow().UtcDateTime);

        // First check: marks the exceeded time.
        var firstResult = await _tracker.CheckBudgetAsync("pause-agent", budget);
        firstResult.IsAllowed.Should().BeFalse();
        firstResult.IsPaused.Should().BeTrue();
        firstResult.ResumeAfter.Should().Be(TimeSpan.FromMinutes(30));

        // Advance 10 minutes (still within pause window).
        _timeProvider.Advance(TimeSpan.FromMinutes(10));

        var secondResult = await _tracker.CheckBudgetAsync("pause-agent", budget);
        secondResult.IsAllowed.Should().BeFalse();
        secondResult.IsPaused.Should().BeTrue();
        secondResult.ResumeAfter.Should().BeCloseTo(TimeSpan.FromMinutes(20), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CheckBudget_OverBudgetWithPauseResume_AllowedAfterPauseElapsed()
    {
        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 5000,
            Period: BudgetPeriod.Daily,
            ActionOnExceeded: BudgetAction.PauseResume,
            PauseResumeMinutes: 30);

        await SeedAuditLogAsync("resume-agent", inputTokens: 3000, outputTokens: 3000,
            timestamp: _timeProvider.GetUtcNow().UtcDateTime);

        // First check: marks exceeded.
        await _tracker.CheckBudgetAsync("resume-agent", budget);

        // Advance past the pause duration.
        _timeProvider.Advance(TimeSpan.FromMinutes(31));

        var result = await _tracker.CheckBudgetAsync("resume-agent", budget);
        result.IsAllowed.Should().BeTrue();
        result.IsPaused.Should().BeFalse();
    }

    [Fact]
    public async Task CheckBudget_SessionPeriod_UsesInMemoryCounters()
    {
        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 1000,
            Period: BudgetPeriod.Session);

        // No DB data needed. Record usage via in-memory tracking.
        await _tracker.RecordUsageAsync("session-agent", inputTokens: 300, outputTokens: 200);

        var result = await _tracker.CheckBudgetAsync("session-agent", budget);

        result.IsAllowed.Should().BeTrue();
        result.TokensUsed.Should().Be(500);
        result.TokensRemaining.Should().Be(500);
    }

    [Fact]
    public async Task CheckBudget_SessionPeriod_OverBudget_Blocked()
    {
        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 1000,
            Period: BudgetPeriod.Session,
            ActionOnExceeded: BudgetAction.Stop);

        await _tracker.RecordUsageAsync("session-over", inputTokens: 600, outputTokens: 500);

        var result = await _tracker.CheckBudgetAsync("session-over", budget);

        result.IsAllowed.Should().BeFalse();
        result.IsPaused.Should().BeFalse();
        result.TokensUsed.Should().Be(1100);
    }

    [Fact]
    public async Task RecordUsage_AccumulatesInMemoryCounters()
    {
        await _tracker.RecordUsageAsync("accum-agent", inputTokens: 100, outputTokens: 50);
        await _tracker.RecordUsageAsync("accum-agent", inputTokens: 200, outputTokens: 100);

        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 10_000,
            Period: BudgetPeriod.Session);

        var summary = await _tracker.GetUsageSummaryAsync("accum-agent", budget);

        summary.TotalTokens.Should().Be(450);
        summary.AgentName.Should().Be("accum-agent");
        summary.Period.Should().Be(BudgetPeriod.Session);
    }

    [Fact]
    public async Task GetUsageSummary_DailyPeriod_AggregatesFromDatabase()
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

        await SeedAuditLogAsync("summary-agent", inputTokens: 1000, outputTokens: 500, timestamp: now);
        await SeedAuditLogAsync("summary-agent", inputTokens: 2000, outputTokens: 1000, timestamp: now.AddHours(-1));

        // Seed data from yesterday (should be excluded).
        await SeedAuditLogAsync("summary-agent", inputTokens: 9000, outputTokens: 9000,
            timestamp: now.AddDays(-1).AddHours(-1));

        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 50_000,
            Period: BudgetPeriod.Daily);

        var summary = await _tracker.GetUsageSummaryAsync("summary-agent", budget);

        summary.TotalInputTokens.Should().Be(3000);
        summary.TotalOutputTokens.Should().Be(1500);
        summary.TotalTokens.Should().Be(4500);
        summary.MaxTokensPerPeriod.Should().Be(50_000);
        summary.Period.Should().Be(BudgetPeriod.Daily);
        summary.PeriodStart.Should().Be(now.Date);
        summary.PeriodEnd.Should().Be(now.Date.AddDays(1));
    }

    [Fact]
    public async Task GetUsageSummary_MonthlyPeriod_AggregatesCorrectly()
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime startOfMonth = new(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        await SeedAuditLogAsync("monthly-agent", inputTokens: 5000, outputTokens: 3000, timestamp: startOfMonth.AddDays(2));
        await SeedAuditLogAsync("monthly-agent", inputTokens: 1000, outputTokens: 500, timestamp: now);

        // Seed data from previous month (should be excluded).
        await SeedAuditLogAsync("monthly-agent", inputTokens: 8000, outputTokens: 4000,
            timestamp: startOfMonth.AddDays(-1));

        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 100_000,
            Period: BudgetPeriod.Monthly);

        var summary = await _tracker.GetUsageSummaryAsync("monthly-agent", budget);

        summary.TotalInputTokens.Should().Be(6000);
        summary.TotalOutputTokens.Should().Be(3500);
        summary.TotalTokens.Should().Be(9500);
        summary.PeriodStart.Should().Be(startOfMonth);
    }

    [Fact]
    public async Task CheckBudget_EmptyAgentName_ThrowsArgumentException()
    {
        var budget = new AgentTokenBudget(MaxTokensPerPeriod: 1000);

        Func<Task> act = () => _tracker.CheckBudgetAsync("", budget);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("agentName");
    }

    [Fact]
    public async Task RecordUsage_EmptyAgentName_ThrowsArgumentException()
    {
        Func<Task> act = () => _tracker.RecordUsageAsync("  ", inputTokens: 100, outputTokens: 50);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("agentName");
    }

    [Fact]
    public async Task CheckBudget_DailyPeriodExcludesOtherAgents()
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

        await SeedAuditLogAsync("agent-a", inputTokens: 9000, outputTokens: 9000, timestamp: now);
        await SeedAuditLogAsync("agent-b", inputTokens: 100, outputTokens: 50, timestamp: now);

        var budget = new AgentTokenBudget(
            MaxTokensPerPeriod: 5000,
            Period: BudgetPeriod.Daily);

        // Agent B should be under budget despite agent A being over.
        var result = await _tracker.CheckBudgetAsync("agent-b", budget);

        result.IsAllowed.Should().BeTrue();
        result.TokensUsed.Should().Be(150);
    }

    private async Task SeedAuditLogAsync(
        string agentName,
        int inputTokens,
        int outputTokens,
        DateTime timestamp)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.LlmAuditLogEntries.Add(new LlmAuditLogEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = timestamp,
            Provider = "test-provider",
            Model = "test-model",
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            LatencyMs = 100,
            Success = true,
            AgentName = agentName
        });
        await db.SaveChangesAsync();
    }
}
