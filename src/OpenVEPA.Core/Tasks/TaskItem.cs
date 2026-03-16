using OpenVEPA.Core.Skills;

namespace OpenVEPA.Core.Tasks;

/// <summary>Represents a discrete unit of work assigned to an agent.</summary>
public sealed record TaskItem(
    string Id,
    string Prompt,
    string? AgentName,
    TaskItemStatus Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? Result,
    TokenUsage? TotalUsage);
