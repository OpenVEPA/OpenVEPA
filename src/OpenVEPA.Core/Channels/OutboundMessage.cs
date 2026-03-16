namespace OpenVEPA.Core.Channels;

/// <summary>Represents a message to be sent to an external channel.</summary>
public sealed record OutboundMessage(
    string Content,
    IReadOnlyList<string>? Attachments);
