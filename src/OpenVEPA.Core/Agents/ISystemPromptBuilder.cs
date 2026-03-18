using OpenVEPA.Core.Preferences;

namespace OpenVEPA.Core.Agents;

/// <summary>Builds dynamic system prompts for agents, injecting context-appropriate information.</summary>
public interface ISystemPromptBuilder
{
    /// <summary>Builds a complete system prompt for the given agent with full context.</summary>
    string Build(SystemPromptContext context);
}

/// <summary>All context needed to build a system prompt.</summary>
/// <param name="Agent">The agent for which to build the prompt.</param>
/// <param name="Preferences">User preferences for personalization.</param>
/// <param name="AvailableAgents">All available agents (for orchestrator delegation awareness).</param>
/// <param name="DelegationVisibility">Delegation visibility setting.</param>
/// <param name="CurrentBudgetStatus">Current budget status, if applicable.</param>
public sealed record SystemPromptContext(
    AgentDefinition Agent,
    UserPreferences? Preferences,
    IReadOnlyList<AgentDefinition>? AvailableAgents = null,
    DelegationVisibility DelegationVisibility = DelegationVisibility.Invisible,
    BudgetCheckResult? CurrentBudgetStatus = null);
