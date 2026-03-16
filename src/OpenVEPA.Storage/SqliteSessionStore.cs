using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenVEPA.Core.Sessions;
using OpenVEPA.Core.Skills;
using OpenVEPA.Storage.Entities;

namespace OpenVEPA.Storage;

/// <summary>
/// SQLite-backed session store. Reads go directly to the database.
/// Writes are serialized through the <see cref="DatabaseWriteQueue"/>.
/// </summary>
public sealed class SqliteSessionStore
{
    private readonly DatabaseWriteQueue _writeQueue;
    private readonly IDbContextFactory<OpenVepaDbContext> _dbFactory;
    private readonly ILogger<SqliteSessionStore> _logger;

    public SqliteSessionStore(
        DatabaseWriteQueue writeQueue,
        IDbContextFactory<OpenVepaDbContext> dbFactory,
        ILogger<SqliteSessionStore> logger)
    {
        _writeQueue = writeQueue ?? throw new ArgumentNullException(nameof(writeQueue));
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Creates a new conversation session.</summary>
    public async Task<Session> CreateSessionAsync(
        string? title,
        CancellationToken ct = default)
    {
        var session = new Session(
            Id: Guid.NewGuid().ToString("N"),
            Title: title,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Status: SessionStatus.Active);

        await _writeQueue.EnqueueAndWaitAsync(async db =>
        {
            db.Sessions.Add(ToEntity(session));
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        _logger.LogDebug("Created session {SessionId}", session.Id);
        return session;
    }

    /// <summary>Gets a session by id, or null if not found.</summary>
    public async Task<Session?> GetSessionAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            .ConfigureAwait(false);

        return entity is null ? null : ToDomain(entity);
    }

    /// <summary>Lists all sessions ordered by most recently updated.</summary>
    public async Task<IReadOnlyList<Session>> ListSessionsAsync(
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await db.Sessions
            .AsNoTracking()
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.ConvertAll(ToDomain);
    }

    /// <summary>Adds a message to an existing session.</summary>
    public async Task AddMessageAsync(
        Message message,
        CancellationToken ct = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        await _writeQueue.EnqueueAndWaitAsync(async db =>
        {
            db.Messages.Add(ToMessageEntity(message));

            // Update session timestamp.
            var session = await db.Sessions.FindAsync([message.SessionId], ct).ConfigureAwait(false);
            if (session is not null)
            {
                session.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        _logger.LogDebug("Added message {MessageId} to session {SessionId}", message.Id, message.SessionId);
    }

    /// <summary>Gets messages for a session with pagination support.</summary>
    public async Task<IReadOnlyList<Message>> GetMessagesAsync(
        string sessionId,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await db.Messages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.ConvertAll(ToMessageDomain);
    }

    private static SessionEntity ToEntity(Session session)
    {
        return new SessionEntity
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            Status = session.Status.ToString()
        };
    }

    private static Session ToDomain(SessionEntity entity)
    {
        return new Session(
            Id: entity.Id,
            Title: entity.Title,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt,
            Status: Enum.Parse<SessionStatus>(entity.Status));
    }

    private static MessageEntity ToMessageEntity(Message message)
    {
        return new MessageEntity
        {
            Id = message.Id,
            SessionId = message.SessionId,
            Role = message.Role.ToString(),
            Content = message.Content,
            Timestamp = message.Timestamp,
            InputTokens = message.Usage?.InputTokens ?? 0,
            OutputTokens = message.Usage?.OutputTokens ?? 0
        };
    }

    private static Message ToMessageDomain(MessageEntity entity)
    {
        var usage = (entity.InputTokens > 0 || entity.OutputTokens > 0)
            ? new TokenUsage(entity.InputTokens, entity.OutputTokens, EstimatedCostUsd: null)
            : null;

        return new Message(
            Id: entity.Id,
            SessionId: entity.SessionId,
            Role: Enum.Parse<MessageRole>(entity.Role),
            Content: entity.Content,
            Timestamp: entity.Timestamp,
            Usage: usage);
    }
}
