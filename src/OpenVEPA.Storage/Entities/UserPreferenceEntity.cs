namespace OpenVEPA.Storage.Entities;

/// <summary>EF Core entity representing a single user preference with provenance metadata.</summary>
public sealed class UserPreferenceEntity
{
    /// <summary>The key path, e.g. "food.dietary".</summary>
    public required string Id { get; set; }

    public required string Value { get; set; }
    public required string Category { get; set; }
    public required string Source { get; set; }
    public double Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
