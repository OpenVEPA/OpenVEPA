using OpenVEPA.Core.Skills;

namespace OpenVEPA.Core.Agents;

/// <summary>Encapsulates an agent's response including any skill results.</summary>
public sealed record AgentResponse(
    string Content,
    IReadOnlyList<SkillResult>? SkillResults,
    TokenUsage? TotalTokenUsage);
