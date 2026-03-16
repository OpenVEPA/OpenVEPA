namespace OpenVEPA.Storage.Entities;

/// <summary>EF Core entity representing a discrete unit of work assigned to an agent.</summary>
public sealed class TaskItemEntity
{
    public required string Id { get; set; }
    public required string Prompt { get; set; }
    public string? AgentName { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Result { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
}
