namespace OpenVEPA.Core.Agents;

/// <summary>Per-agent LLM configuration override. Null fields fall back to system defaults.</summary>
public sealed record AgentLlmConfig(
    /// <summary>LLM provider identifier (e.g., "google", "anthropic", "openai").</summary>
    string? Provider = null,
    /// <summary>Model name (e.g., "gemini-2.5-flash", "claude-sonnet-4").</summary>
    string? Model = null,
    /// <summary>Sampling temperature (0.0 – 2.0).</summary>
    double? Temperature = null,
    /// <summary>Maximum response tokens.</summary>
    int? MaxTokens = null,
    /// <summary>Optional fallback LLM configuration used when the primary provider/model is unavailable.</summary>
    AgentLlmConfig? Fallback = null);
