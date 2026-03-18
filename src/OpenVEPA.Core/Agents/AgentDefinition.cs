namespace OpenVEPA.Core.Agents;

/// <summary>Declares an agent's identity, behavior, and skill set.</summary>
public sealed record AgentDefinition(
    /// <summary>Unique agent name.</summary>
    string Name,
    /// <summary>Human-readable description.</summary>
    string Description,
    /// <summary>System prompt that governs agent behavior.</summary>
    string SystemPrompt,
    /// <summary>Names of skills this agent may invoke.</summary>
    IReadOnlyList<string> Skills,
    /// <summary>Autonomy level (0 = fully supervised, higher = more autonomous).</summary>
    int AutonomyLevel,
    /// <summary>LLM capability requirements.</summary>
    AgentLlmRequirements? LlmRequirements,
    /// <summary>Whether this is a built-in system agent.</summary>
    bool IsSystem = false,
    /// <summary>Per-agent LLM configuration override.</summary>
    AgentLlmConfig? LlmConfig = null,
    /// <summary>Free-text restrictions describing what the agent must not do.</summary>
    IReadOnlyList<string>? Restrictions = null,
    /// <summary>Access permissions for this agent.</summary>
    AgentPermissions? Permissions = null,
    /// <summary>Phrases that trigger delegation to this agent.</summary>
    IReadOnlyList<string>? Triggers = null,
    /// <summary>Priority for delegation selection. Higher values are preferred.</summary>
    int Priority = 10,
    /// <summary>Token budget configuration.</summary>
    AgentTokenBudget? TokenBudget = null);
