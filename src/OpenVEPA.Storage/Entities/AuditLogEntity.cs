namespace OpenVEPA.Storage.Entities;

/// <summary>EF Core entity recording a significant system action for audit purposes.</summary>
public sealed class AuditLogEntity
{
    public required string Id { get; set; }
    public required string Action { get; set; }
    public string? SessionId { get; set; }
    public string? TaskId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
