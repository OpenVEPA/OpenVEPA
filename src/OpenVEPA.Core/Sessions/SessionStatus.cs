namespace OpenVEPA.Core.Sessions;

/// <summary>Tracks the lifecycle state of a conversation session.</summary>
public enum SessionStatus
{
    /// <summary>Session is actively receiving messages.</summary>
    Active,

    /// <summary>Session is temporarily paused.</summary>
    Paused,

    /// <summary>Session has been completed normally.</summary>
    Completed,

    /// <summary>Session has been archived for long-term storage.</summary>
    Archived
}
