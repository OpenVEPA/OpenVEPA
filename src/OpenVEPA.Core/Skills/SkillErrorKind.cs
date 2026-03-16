namespace OpenVEPA.Core.Skills;

/// <summary>Categorizes skill execution errors for retry and reporting decisions.</summary>
public enum SkillErrorKind
{
    /// <summary>Temporary failure that may succeed on retry.</summary>
    Transient,

    /// <summary>Permanent failure that will not succeed on retry.</summary>
    Permanent,

    /// <summary>Rate limit exceeded; back off before retrying.</summary>
    RateLimited,

    /// <summary>Authentication or authorization failed.</summary>
    AuthFailed,

    /// <summary>Operation exceeded the allowed time limit.</summary>
    Timeout
}
