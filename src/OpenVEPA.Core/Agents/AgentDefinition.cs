namespace OpenVEPA.Core.Agents;

/// <summary>Declares an agent's identity, behavior, and skill set.</summary>
public sealed record AgentDefinition(
    string Name,
    string Description,
    string SystemPrompt,
    IReadOnlyList<string> Skills,
    int AutonomyLevel,
    AgentLlmRequirements? LlmRequirements);
