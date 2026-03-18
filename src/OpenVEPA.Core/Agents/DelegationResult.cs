using OpenVEPA.Core.Skills;

namespace OpenVEPA.Core.Agents;

/// <summary>Result of delegating a task to a sub-agent.</summary>
public sealed record DelegationResult(
    /// <summary>Name of the agent that handled the delegation.</summary>
    string AgentName,
    /// <summary>The agent's response text.</summary>
    string Response,
    /// <summary>Token usage incurred during delegation.</summary>
    TokenUsage? TokensUsed,
    /// <summary>Whether the delegation completed successfully.</summary>
    bool Success,
    /// <summary>Error message if the delegation failed.</summary>
    string? ErrorMessage = null,
    /// <summary>Wall-clock duration of the delegation.</summary>
    TimeSpan Duration = default);
