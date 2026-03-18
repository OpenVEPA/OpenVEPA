using FluentAssertions;
using OpenVEPA.Core.Agents;

namespace OpenVEPA.Agents.Runtime.Tests;

/// <summary>Tests for the extended frontmatter fields in <see cref="AgentMdParser"/>.</summary>
public sealed class AgentMdParserExtendedTests
{
    private readonly AgentMdParser _parser = new();

    // ──────────────────────────────────────────────
    // Full .agent.md with ALL new fields
    // ──────────────────────────────────────────────

    [Fact]
    public void Parse_AllNewFields_PopulatesEveryProperty()
    {
        const string content = """
            ---
            name: research-agent
            description: Research specialist
            openvepa-skills: [web-search, summarize]
            openvepa-autonomy-default: 3
            openvepa-priority: 20
            openvepa-triggers:
              - research
              - find information
              - look up
            openvepa-restrictions:
              - Never share personal data
              - Do not make purchases
            openvepa-llm:
              provider: google
              model: gemini-2.5-flash
              temperature: 0.7
              max-tokens: 4096
            openvepa-permissions:
              internet: true
              file-system: false
              code-execution: false
              database-access: false
            openvepa-token-budget:
              max-tokens-per-period: 1000000
              period: monthly
              action-on-exceeded: pause-resume
              pause-resume-minutes: 30
            ---

            ## System Prompt
            You are a research agent.
            """;

        var result = _parser.Parse("research.agent.md", content);

        result.Name.Should().Be("research-agent");
        result.Description.Should().Be("Research specialist");
        result.AutonomyLevel.Should().Be(3);
        result.Skills.Should().Equal("web-search", "summarize");

        // Priority
        result.Priority.Should().Be(20);

        // Triggers
        result.Triggers.Should().NotBeNull();
        result.Triggers!.Should().Equal("research", "find information", "look up");

        // Restrictions
        result.Restrictions.Should().NotBeNull();
        result.Restrictions!.Should().Equal("Never share personal data", "Do not make purchases");

        // LLM Config
        result.LlmConfig.Should().NotBeNull();
        result.LlmConfig!.Provider.Should().Be("google");
        result.LlmConfig.Model.Should().Be("gemini-2.5-flash");
        result.LlmConfig.Temperature.Should().Be(0.7);
        result.LlmConfig.MaxTokens.Should().Be(4096);

        // Permissions
        result.Permissions.Should().NotBeNull();
        result.Permissions!.Internet.Should().BeTrue();
        result.Permissions.FileSystem.Should().BeFalse();
        result.Permissions.CodeExecution.Should().BeFalse();
        result.Permissions.DatabaseAccess.Should().BeFalse();

        // Token Budget
        result.TokenBudget.Should().NotBeNull();
        result.TokenBudget!.MaxTokensPerPeriod.Should().Be(1_000_000);
        result.TokenBudget.Period.Should().Be(BudgetPeriod.Monthly);
        result.TokenBudget.ActionOnExceeded.Should().Be(BudgetAction.PauseResume);
        result.TokenBudget.PauseResumeMinutes.Should().Be(30);
    }

    // ──────────────────────────────────────────────
    // Partial fields: only some new fields present
    // ──────────────────────────────────────────────

    [Fact]
    public void Parse_OnlyPriorityAndTriggers_OtherNewFieldsAreNull()
    {
        const string content = """
            ---
            name: helper
            description: A helper agent
            openvepa-priority: 5
            openvepa-triggers:
              - help me
            ---

            ## System Prompt
            Help the user.
            """;

        var result = _parser.Parse("helper.agent.md", content);

        result.Priority.Should().Be(5);
        result.Triggers.Should().Equal("help me");

        result.LlmConfig.Should().BeNull();
        result.Restrictions.Should().BeNull();
        result.Permissions.Should().BeNull();
        result.TokenBudget.Should().BeNull();
    }

    [Fact]
    public void Parse_OnlyPermissions_OtherNewFieldsHaveDefaults()
    {
        const string content = """
            ---
            name: sandbox
            description: Sandboxed agent
            openvepa-permissions:
              internet: false
              file-system: true
              code-execution: true
              database-access: false
            ---

            ## System Prompt
            Run safely.
            """;

        var result = _parser.Parse("sandbox.agent.md", content);

        result.Permissions.Should().NotBeNull();
        result.Permissions!.Internet.Should().BeFalse();
        result.Permissions.FileSystem.Should().BeTrue();
        result.Permissions.CodeExecution.Should().BeTrue();
        result.Permissions.DatabaseAccess.Should().BeFalse();

        result.Priority.Should().Be(10);
        result.Triggers.Should().BeNull();
        result.Restrictions.Should().BeNull();
        result.LlmConfig.Should().BeNull();
        result.TokenBudget.Should().BeNull();
    }

    // ──────────────────────────────────────────────
    // Backward compatibility: no new fields at all
    // ──────────────────────────────────────────────

    [Fact]
    public void Parse_NoNewFields_BackwardCompatible()
    {
        const string content = """
            ---
            name: classic-agent
            description: Old-style agent
            openvepa-skills: [search]
            openvepa-autonomy-default: 1
            ---

            ## System Prompt
            Classic behavior.
            """;

        var result = _parser.Parse("classic.agent.md", content);

        result.Name.Should().Be("classic-agent");
        result.Description.Should().Be("Old-style agent");
        result.Skills.Should().Equal("search");
        result.AutonomyLevel.Should().Be(1);

        // All new fields should be null or default
        result.Priority.Should().Be(10);
        result.Triggers.Should().BeNull();
        result.Restrictions.Should().BeNull();
        result.LlmConfig.Should().BeNull();
        result.Permissions.Should().BeNull();
        result.TokenBudget.Should().BeNull();
    }

    // ──────────────────────────────────────────────
    // Edge cases: enum parsing
    // ──────────────────────────────────────────────

    [Fact]
    public void Parse_TokenBudget_SessionPeriodAndStopAction()
    {
        const string content = """
            ---
            name: limited
            description: Limited agent
            openvepa-token-budget:
              max-tokens-per-period: 500
              period: session
              action-on-exceeded: stop
              pause-resume-minutes: 10
            ---
            """;

        var result = _parser.Parse("limited.agent.md", content);

        result.TokenBudget.Should().NotBeNull();
        result.TokenBudget!.MaxTokensPerPeriod.Should().Be(500);
        result.TokenBudget.Period.Should().Be(BudgetPeriod.Session);
        result.TokenBudget.ActionOnExceeded.Should().Be(BudgetAction.Stop);
        result.TokenBudget.PauseResumeMinutes.Should().Be(10);
    }

    [Fact]
    public void Parse_TokenBudget_DailyPeriod()
    {
        const string content = """
            ---
            name: daily
            description: Daily budget agent
            openvepa-token-budget:
              max-tokens-per-period: 10000
              period: daily
              action-on-exceeded: pause-resume
              pause-resume-minutes: 5
            ---
            """;

        var result = _parser.Parse("daily.agent.md", content);

        result.TokenBudget.Should().NotBeNull();
        result.TokenBudget!.Period.Should().Be(BudgetPeriod.Daily);
        result.TokenBudget.ActionOnExceeded.Should().Be(BudgetAction.PauseResume);
    }

    [Fact]
    public void Parse_TokenBudget_InvalidEnumFallsBackToDefault()
    {
        const string content = """
            ---
            name: bad-enums
            description: Agent with invalid enum values
            openvepa-token-budget:
              max-tokens-per-period: 100
              period: biweekly
              action-on-exceeded: explode
            ---
            """;

        var result = _parser.Parse("bad-enums.agent.md", content);

        result.TokenBudget.Should().NotBeNull();
        result.TokenBudget!.MaxTokensPerPeriod.Should().Be(100);
        result.TokenBudget.Period.Should().Be(BudgetPeriod.Monthly, "invalid enum should default to Monthly");
        result.TokenBudget.ActionOnExceeded.Should().Be(BudgetAction.Stop, "invalid enum should default to Stop");
        result.TokenBudget.PauseResumeMinutes.Should().Be(60, "missing key should use record default");
    }

    // ──────────────────────────────────────────────
    // Edge cases: nested object parsing
    // ──────────────────────────────────────────────

    [Fact]
    public void Parse_LlmConfig_PartialFields()
    {
        const string content = """
            ---
            name: partial-llm
            description: Agent with partial LLM config
            openvepa-llm:
              model: claude-sonnet-4
            ---
            """;

        var result = _parser.Parse("partial-llm.agent.md", content);

        result.LlmConfig.Should().NotBeNull();
        result.LlmConfig!.Provider.Should().BeNull();
        result.LlmConfig.Model.Should().Be("claude-sonnet-4");
        result.LlmConfig.Temperature.Should().BeNull();
        result.LlmConfig.MaxTokens.Should().BeNull();
    }

    [Fact]
    public void Parse_Permissions_PartialFields_MissingDefaultToFalse()
    {
        const string content = """
            ---
            name: partial-perms
            description: Agent with some permissions
            openvepa-permissions:
              internet: true
            ---
            """;

        var result = _parser.Parse("partial-perms.agent.md", content);

        result.Permissions.Should().NotBeNull();
        result.Permissions!.Internet.Should().BeTrue();
        result.Permissions.FileSystem.Should().BeFalse("missing permission should default to false");
        result.Permissions.CodeExecution.Should().BeFalse("missing permission should default to false");
        result.Permissions.DatabaseAccess.Should().BeFalse("missing permission should default to false");
    }

    [Fact]
    public void Parse_TokenBudget_PartialFields_MissingUseRecordDefaults()
    {
        const string content = """
            ---
            name: minimal-budget
            description: Agent with minimal budget config
            openvepa-token-budget:
              max-tokens-per-period: 50000
            ---
            """;

        var result = _parser.Parse("minimal-budget.agent.md", content);

        result.TokenBudget.Should().NotBeNull();
        result.TokenBudget!.MaxTokensPerPeriod.Should().Be(50_000);
        result.TokenBudget.Period.Should().Be(BudgetPeriod.Monthly, "missing should use default");
        result.TokenBudget.ActionOnExceeded.Should().Be(BudgetAction.Stop, "missing should use default");
        result.TokenBudget.PauseResumeMinutes.Should().Be(60, "missing should use default");
    }

    // ──────────────────────────────────────────────
    // Edge cases: empty lists return null
    // ──────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyTriggersAndRestrictions_ReturnsNull()
    {
        const string content = """
            ---
            name: empty-lists
            description: Agent with empty list values
            openvepa-triggers: []
            openvepa-restrictions: []
            ---
            """;

        var result = _parser.Parse("empty-lists.agent.md", content);

        result.Triggers.Should().BeNull("empty list should become null");
        result.Restrictions.Should().BeNull("empty list should become null");
    }

    // ──────────────────────────────────────────────
    // Edge case: priority as string
    // ──────────────────────────────────────────────

    [Fact]
    public void Parse_PriorityAsString_ParsesCorrectly()
    {
        const string content = """
            ---
            name: string-priority
            description: Agent with string priority
            openvepa-priority: "15"
            ---
            """;

        var result = _parser.Parse("string-priority.agent.md", content);

        result.Priority.Should().Be(15);
    }

    // ──────────────────────────────────────────────
    // Edge case: permissions with string booleans
    // ──────────────────────────────────────────────

    [Fact]
    public void Parse_Permissions_StringBooleans_ParsedCorrectly()
    {
        const string content = """
            ---
            name: string-bools
            description: Agent with string boolean permissions
            openvepa-permissions:
              internet: "true"
              file-system: "false"
              code-execution: "True"
              database-access: "FALSE"
            ---
            """;

        var result = _parser.Parse("string-bools.agent.md", content);

        result.Permissions.Should().NotBeNull();
        result.Permissions!.Internet.Should().BeTrue();
        result.Permissions.FileSystem.Should().BeFalse();
        result.Permissions.CodeExecution.Should().BeTrue();
        result.Permissions.DatabaseAccess.Should().BeFalse();
    }

    // ──────────────────────────────────────────────
    // Edge case: LLM config temperature as integer
    // ──────────────────────────────────────────────

    [Fact]
    public void Parse_LlmConfig_IntegerTemperature_ParsedAsDouble()
    {
        const string content = """
            ---
            name: int-temp
            description: Agent with integer temperature
            openvepa-llm:
              provider: openai
              model: gpt-4o
              temperature: 1
              max-tokens: 2048
            ---
            """;

        var result = _parser.Parse("int-temp.agent.md", content);

        result.LlmConfig.Should().NotBeNull();
        result.LlmConfig!.Temperature.Should().Be(1.0);
        result.LlmConfig.MaxTokens.Should().Be(2048);
    }
}
