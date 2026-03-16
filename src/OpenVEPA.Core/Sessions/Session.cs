namespace OpenVEPA.Core.Sessions;

/// <summary>Represents a conversation session with lifecycle tracking.</summary>
public sealed record Session(
    string Id,
    string? Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SessionStatus Status);
