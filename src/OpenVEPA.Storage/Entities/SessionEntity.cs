namespace OpenVEPA.Storage.Entities;

/// <summary>EF Core entity representing a conversation session.</summary>
public sealed class SessionEntity
{
    public required string Id { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string Status { get; set; }

    public List<MessageEntity> Messages { get; set; } = [];
}
