using System.Text;

using OpenVEPA.Core.Agents;

namespace OpenVEPA.Agents.Runtime;

/// <summary>
/// Builds dynamic system prompts by composing agent identity, user preferences,
/// specialist awareness, and budget context into a single prompt string.
/// </summary>
internal sealed class SystemPromptBuilder : ISystemPromptBuilder
{
    /// <inheritdoc />
    public string Build(SystemPromptContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var sb = new StringBuilder();

        AppendBasePrompt(sb, context.Agent);
        AppendUserPreferences(sb, context.Preferences);
        AppendRestrictions(sb, context.Agent);

        if (context.AvailableAgents is { Count: > 0 })
        {
            AppendAvailableAgents(sb, context.AvailableAgents, context.Agent);
            AppendDecisionFramework(sb);
        }

        if (context.CurrentBudgetStatus is not null)
            AppendBudgetStatus(sb, context.CurrentBudgetStatus);

        AppendDelegationVisibility(sb, context.DelegationVisibility);

        return sb.ToString().TrimEnd();
    }

    private static void AppendBasePrompt(StringBuilder sb, AgentDefinition agent)
    {
        sb.AppendLine(agent.SystemPrompt);
    }

    private static void AppendUserPreferences(
        StringBuilder sb,
        Core.Preferences.UserPreferences? preferences)
    {
        if (preferences is not { Entries.Count: > 0 })
            return;

        sb.AppendLine();
        sb.AppendLine("## User Preferences");
        foreach (var (key, entry) in preferences.Entries)
            sb.AppendLine($"- {key}: {entry.Value}");
    }

    private static void AppendRestrictions(StringBuilder sb, AgentDefinition agent)
    {
        if (agent.Restrictions is not { Count: > 0 })
            return;

        sb.AppendLine();
        sb.AppendLine("## Restrictions");
        foreach (var restriction in agent.Restrictions)
            sb.AppendLine($"- {restriction}");
    }

    private static void AppendAvailableAgents(
        StringBuilder sb,
        IReadOnlyList<AgentDefinition> availableAgents,
        AgentDefinition currentAgent)
    {
        var specialists = availableAgents
            .Where(a => !string.Equals(a.Name, currentAgent.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.Priority)
            .ToList();

        if (specialists.Count == 0)
            return;

        sb.AppendLine();
        sb.AppendLine("## Available Specialists");
        sb.AppendLine("You can delegate tasks to these specialist agents:");

        foreach (var agent in specialists)
        {
            sb.AppendLine();
            sb.AppendLine($"### {agent.Name} (Priority: {agent.Priority})");
            sb.AppendLine($"**Description**: {agent.Description}");

            if (agent.Triggers is { Count: > 0 })
                sb.AppendLine($"**Triggers**: {string.Join(", ", agent.Triggers)}");

            if (agent.Skills.Count > 0)
                sb.AppendLine($"**Skills**: {string.Join(", ", agent.Skills)}");

            if (agent.Permissions is { } perms)
            {
                sb.AppendLine(
                    $"**Permissions**: Internet: {FormatBool(perms.Internet)}, " +
                    $"FileSystem: {FormatBool(perms.FileSystem)}, " +
                    $"CodeExecution: {FormatBool(perms.CodeExecution)}, " +
                    $"Database: {FormatBool(perms.DatabaseAccess)}");
            }
        }
    }

    private static void AppendDecisionFramework(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("## Decision Framework");
        sb.AppendLine("When processing a user message, follow this framework:");
        sb.AppendLine("1. **Classify intent**: Determine what the user wants (conversation, task, information)");
        sb.AppendLine("2. **Check triggers**: Match the message against specialist agent triggers");
        sb.AppendLine("3. **Evaluate complexity**: Simple greetings/conversations → handle directly");
        sb.AppendLine("4. **Delegate if appropriate**: If a specialist matches and the task is within their domain, delegate");
        sb.AppendLine("5. **Handle directly**: If no specialist matches or the task is general conversation");
        sb.AppendLine("6. **Combine results**: If multiple specialists are needed, orchestrate sequential delegation");
        sb.AppendLine();
        sb.AppendLine("When delegating, provide clear task descriptions. Do not delegate simple greetings or meta-questions about yourself.");
    }

    private static void AppendBudgetStatus(StringBuilder sb, BudgetCheckResult budget)
    {
        var total = budget.TokensUsed + budget.TokensRemaining;
        var percentage = total > 0
            ? (int)(budget.TokensUsed * 100 / total)
            : 0;

        sb.AppendLine();
        sb.AppendLine("## Budget Status");
        sb.AppendLine($"- Tokens used: {budget.TokensUsed:N0} / {total:N0} ({percentage}% of budget)");

        if (budget.IsPaused && budget.ResumeAfter.HasValue)
            sb.AppendLine($"- Status: Paused (resumes in {budget.ResumeAfter.Value.TotalMinutes:N0} minutes)");
        else if (!budget.IsAllowed)
            sb.AppendLine("- Status: Budget exceeded");
        else if (percentage >= 80)
            sb.AppendLine("- Status: Warning, approaching budget limit");
        else
            sb.AppendLine("- Status: OK");

        if (budget.Message is { Length: > 0 })
            sb.AppendLine($"- Note: {budget.Message}");
    }

    private static void AppendDelegationVisibility(
        StringBuilder sb,
        DelegationVisibility visibility)
    {
        sb.AppendLine();
        sb.AppendLine("## Delegation Presentation");

        var instruction = visibility switch
        {
            DelegationVisibility.Invisible =>
                "Do NOT mention that you delegated to another agent. Present the response as your own.",
            DelegationVisibility.Visible =>
                "Briefly note which agent you consulted, but keep the focus on the answer.",
            DelegationVisibility.Detailed =>
                "Show the full delegation chain, which agent handled what, and token usage.",
            _ => "Present the response as your own."
        };

        sb.AppendLine(instruction);
    }

    private static string FormatBool(bool value) => value ? "Yes" : "No";
}
