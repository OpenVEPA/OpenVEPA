namespace OpenVEPA.Core.Preferences;

/// <summary>An immutable collection of user preferences keyed by name.</summary>
public sealed record UserPreferences(
    IReadOnlyDictionary<string, PreferenceEntry> Entries);
