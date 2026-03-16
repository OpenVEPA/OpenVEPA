namespace OpenVEPA.Core.Audit;

/// <summary>Records an LLM invocation with cost, latency, and outcome details.</summary>
public sealed record LlmAuditLogEntry(
    string Id,
    DateTime Timestamp,
    string Provider,
    string Model,
    int InputTokens,
    int OutputTokens,
    decimal? EstimatedCostUsd,
    int LatencyMs,
    string? SessionId,
    string? TaskId,
    string? SkillName,
    bool Success,
    string? ErrorMessage);
