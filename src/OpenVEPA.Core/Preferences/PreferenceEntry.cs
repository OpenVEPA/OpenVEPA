namespace OpenVEPA.Core.Preferences;

/// <summary>A single user preference with provenance and confidence metadata.</summary>
public sealed record PreferenceEntry(
    string Key,
    string Value,
    string Category,
    PreferenceSource Source,
    double Confidence,
    DateTime UpdatedAt);
