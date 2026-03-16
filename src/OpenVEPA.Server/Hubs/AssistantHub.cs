using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OpenVEPA.Core.Agents;
using OpenVEPA.Core.Sessions;

namespace OpenVEPA.Server.Hubs;

/// <summary>
/// SignalR hub for the assistant chat interface.
/// Delegates message processing to <see cref="IAgentRuntime"/>
/// and session management to <see cref="ISessionStore"/>.
/// </summary>
[Authorize]
public sealed class AssistantHub : Hub
{
    private readonly IAgentRuntime _agentRuntime;
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<AssistantHub> _logger;

    public AssistantHub(
        IAgentRuntime agentRuntime,
        ISessionStore sessionStore,
        ILogger<AssistantHub> logger)
    {
        _agentRuntime = agentRuntime ?? throw new ArgumentNullException(nameof(agentRuntime));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a message and returns the complete response.
    /// Stores both user and assistant messages in the session.
    /// </summary>
    public async Task<string> SendMessage(string sessionId, string message)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new HubException("Session id is required.");
        }

        if (string.IsNullOrEmpty(message))
        {
            throw new HubException("Message is required.");
        }

        _logger.LogDebug(
            "SendMessage from {ConnectionId} in session {SessionId}",
            Context.ConnectionId,
            sessionId);

        await StoreUserMessageAsync(sessionId, message).ConfigureAwait(false);

        var response = await _agentRuntime
            .ProcessMessageAsync(sessionId, message, Context.ConnectionAborted)
            .ConfigureAwait(false);

        await StoreAssistantMessageAsync(sessionId, response.Content, response.TotalTokenUsage)
            .ConfigureAwait(false);

        return response.Content;
    }

    /// <summary>
    /// Streams response tokens for a user message via <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    public async IAsyncEnumerable<string> StreamMessage(
        string sessionId,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new HubException("Session id is required.");
        }

        if (string.IsNullOrEmpty(message))
        {
            throw new HubException("Message is required.");
        }

        _logger.LogDebug(
            "StreamMessage from {ConnectionId} in session {SessionId}",
            Context.ConnectionId,
            sessionId);

        await StoreUserMessageAsync(sessionId, message).ConfigureAwait(false);

        await foreach (var token in _agentRuntime
            .StreamResponseAsync(sessionId, message, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return token;
        }
    }

    /// <summary>Creates a new conversation session.</summary>
    public async Task<string> CreateSession(string? title)
    {
        var session = await _sessionStore
            .CreateSessionAsync(title, Context.ConnectionAborted)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Session {SessionId} created by {ConnectionId}",
            session.Id,
            Context.ConnectionId);

        return session.Id;
    }

    /// <summary>Lists all sessions ordered by most recently updated.</summary>
    public async Task<IReadOnlyList<Session>> ListSessions()
    {
        return await _sessionStore
            .ListSessionsAsync(Context.ConnectionAborted)
            .ConfigureAwait(false);
    }

    /// <summary>Gets paginated message history for a session.</summary>
    public async Task<IReadOnlyList<Message>> GetSessionHistory(
        string sessionId,
        int page = 1,
        int pageSize = 50)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new HubException("Session id is required.");
        }

        if (page < 1)
        {
            throw new HubException("Page must be >= 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new HubException("Page size must be between 1 and 100.");
        }

        var skip = (page - 1) * pageSize;
        return await _sessionStore
            .GetMessagesAsync(sessionId, skip, pageSize, Context.ConnectionAborted)
            .ConfigureAwait(false);
    }

    private async Task StoreUserMessageAsync(string sessionId, string content)
    {
        var userMessage = new Message(
            Id: Guid.NewGuid().ToString("N"),
            SessionId: sessionId,
            Role: MessageRole.User,
            Content: content,
            Timestamp: DateTime.UtcNow,
            Usage: null);

        await _sessionStore.AddMessageAsync(userMessage, Context.ConnectionAborted)
            .ConfigureAwait(false);
    }

    private async Task StoreAssistantMessageAsync(
        string sessionId,
        string content,
        Core.Skills.TokenUsage? usage)
    {
        var assistantMessage = new Message(
            Id: Guid.NewGuid().ToString("N"),
            SessionId: sessionId,
            Role: MessageRole.Assistant,
            Content: content,
            Timestamp: DateTime.UtcNow,
            Usage: usage);

        await _sessionStore.AddMessageAsync(assistantMessage, Context.ConnectionAborted)
            .ConfigureAwait(false);
    }
}
