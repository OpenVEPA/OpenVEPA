namespace OpenVEPA.Core.Skills;

/// <summary>Tracks token consumption and estimated cost for an LLM call.</summary>
public sealed record TokenUsage(
    int InputTokens,
    int OutputTokens,
    decimal? EstimatedCostUsd);
