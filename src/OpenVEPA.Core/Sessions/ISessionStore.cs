namespace OpenVEPA.Core.Sessions;

/// <summary>Defines persistence operations for conversation sessions and their messages.</summary>
public interface ISessionStore
{
    /// <summary>Creates a new conversation session.</summary>
    /// <param name="title">The optional session title.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The created <see cref="Session"/>.</returns>
    Task<Session> CreateSessionAsync(string? title, CancellationToken ct = default);

    /// <summary>Gets a session by identifier.</summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The matching <see cref="Session"/>, or <see langword="null"/> when not found.</returns>
    Task<Session?> GetSessionAsync(string id, CancellationToken ct = default);

    /// <summary>Lists all sessions.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The stored sessions.</returns>
    Task<IReadOnlyList<Session>> ListSessionsAsync(CancellationToken ct = default);

    /// <summary>Adds a message to an existing session.</summary>
    /// <param name="message">The message to persist.</param>
    /// <param name="ct">A cancellation token.</param>
    Task AddMessageAsync(Message message, CancellationToken ct = default);

    /// <summary>Gets messages for a session using pagination.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The number of messages to return.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The requested page of messages.</returns>
    Task<IReadOnlyList<Message>> GetMessagesAsync(
        string sessionId,
        int skip,
        int take,
        CancellationToken ct = default);
}
