namespace OpenVEPA.Core.Channels;

/// <summary>Adapts an external messaging platform to the OpenVEPA pipeline.</summary>
public interface IChannelAdapter : IAsyncDisposable
{
    /// <summary>Gets the unique identifier for this channel.</summary>
    string ChannelId { get; }

    /// <summary>Starts listening for inbound messages.</summary>
    Task StartAsync(CancellationToken ct);

    /// <summary>Stops listening and releases channel resources.</summary>
    Task StopAsync(CancellationToken ct);

    /// <summary>Sends a message to a session participant on this channel.</summary>
    Task SendMessageAsync(string sessionId, OutboundMessage message, CancellationToken ct);

    /// <summary>Raised when a message is received from the channel.</summary>
    event Func<InboundMessage, Task>? OnMessageReceived;
}
