namespace OpenVEPA.Core.Preferences;

/// <summary>Manages the current user's preference profile.</summary>
public interface IUserProfileService
{
    /// <summary>Gets preferences relevant to the specified task domain.</summary>
    Task<UserPreferences> GetRelevantPreferencesAsync(string? taskDomain, CancellationToken ct);

    /// <summary>Sets an explicit preference value.</summary>
    Task SetExplicitPreferenceAsync(string key, string value, string category, CancellationToken ct);

    /// <summary>Deletes a preference by key.</summary>
    Task DeletePreferenceAsync(string key, CancellationToken ct);

    /// <summary>Gets all stored preferences.</summary>
    Task<IReadOnlyList<PreferenceEntry>> GetAllPreferencesAsync(CancellationToken ct);
}
