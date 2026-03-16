namespace OpenVEPA.Core.Audit;

/// <summary>Records a significant system action for audit trail purposes.</summary>
public sealed record AuditLogEntry(
    string Id,
    string Action,
    string? SessionId,
    string? TaskId,
    string? Details,
    DateTime Timestamp);
