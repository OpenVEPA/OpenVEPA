namespace OpenVEPA.Storage.Entities;

/// <summary>EF Core entity storing a hashed API access token with lifecycle metadata.</summary>
public sealed class AccessTokenEntity
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string HashedToken { get; set; }
    public required string Salt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
