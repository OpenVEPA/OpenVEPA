using OpenVEPA.Core.Skills;

namespace OpenVEPA.Core.Sessions;

/// <summary>Represents a single message within a conversation session.</summary>
public sealed record Message(
    string Id,
    string SessionId,
    MessageRole Role,
    string Content,
    DateTime Timestamp,
    TokenUsage? Usage);
