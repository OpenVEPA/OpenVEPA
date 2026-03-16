namespace OpenVEPA.Core.Preferences;

/// <summary>Identifies how a user preference was established.</summary>
public enum PreferenceSource
{
    /// <summary>User explicitly set the preference.</summary>
    Explicit,

    /// <summary>System inferred the preference from behavior.</summary>
    Inferred,

    /// <summary>System-inferred preference confirmed by the user.</summary>
    Confirmed
}
