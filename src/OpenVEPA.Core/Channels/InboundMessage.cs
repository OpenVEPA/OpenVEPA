namespace OpenVEPA.Core.Channels;

/// <summary>Represents a message received from an external channel.</summary>
public sealed record InboundMessage(
    string ChannelId,
    string PlatformUserId,
    string Content,
    DateTime Timestamp,
    IDictionary<string, string>? Metadata);
