namespace OpenVEPA.Core.Agents;

/// <summary>Specifies the LLM capabilities an agent requires.</summary>
public sealed record AgentLlmRequirements(
    IReadOnlyList<string> Capabilities);
