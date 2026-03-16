namespace OpenVEPA.Storage.Entities;

/// <summary>EF Core entity representing a single message within a session.</summary>
public sealed class MessageEntity
{
    public required string Id { get; set; }
    public required string SessionId { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }

    public SessionEntity? Session { get; set; }
}
