namespace OpenVEPA.Storage.Entities;

/// <summary>EF Core entity recording an LLM invocation with cost, latency, and outcome details.</summary>
public sealed class LlmAuditLogEntity
{
    public required string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Provider { get; set; }
    public required string Model { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public int LatencyMs { get; set; }
    public string? SessionId { get; set; }
    public string? TaskId { get; set; }
    public string? SkillName { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
