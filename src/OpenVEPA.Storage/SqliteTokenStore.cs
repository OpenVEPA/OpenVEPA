using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenVEPA.Storage.Entities;

namespace OpenVEPA.Storage;

/// <summary>Result of token creation. The plaintext token is available only at creation time.</summary>
public sealed record TokenCreateResult(string TokenId, string PlaintextToken);

/// <summary>Read-only metadata about an access token. Never exposes hash or salt.</summary>
public sealed record TokenInfo(
    string Id,
    string Name,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    bool IsRevoked);

/// <summary>
/// SQLite-backed token store using PBKDF2 (HMAC-SHA256) for secure token hashing.
/// Plaintext tokens are returned only at creation time and never stored.
/// </summary>
public sealed class SqliteTokenStore
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int TokenSizeBytes = 32;
    private const int Pbkdf2Iterations = 100_000;

    private readonly DatabaseWriteQueue _writeQueue;
    private readonly IDbContextFactory<OpenVepaDbContext> _dbFactory;
    private readonly ILogger<SqliteTokenStore> _logger;

    public SqliteTokenStore(
        DatabaseWriteQueue writeQueue,
        IDbContextFactory<OpenVepaDbContext> dbFactory,
        ILogger<SqliteTokenStore> logger)
    {
        _writeQueue = writeQueue ?? throw new ArgumentNullException(nameof(writeQueue));
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new access token. The returned plaintext token is available only once.
    /// </summary>
    public async Task<TokenCreateResult> CreateTokenAsync(
        string name,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Token name is required.", nameof(name));
        }

        var tokenId = Guid.NewGuid().ToString("N");
        var plaintextBytes = RandomNumberGenerator.GetBytes(TokenSizeBytes);
        var plaintext = Convert.ToBase64String(plaintextBytes);

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = HashToken(plaintextBytes, salt);

        var entity = new AccessTokenEntity
        {
            Id = tokenId,
            Name = name,
            HashedToken = Convert.ToBase64String(hash),
            Salt = Convert.ToBase64String(salt),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _writeQueue.EnqueueAndWaitAsync(async db =>
            {
                db.AccessTokens.Add(entity);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            _logger.LogInformation("Token created: {TokenId}, name={Name}", tokenId, name);
            return new TokenCreateResult(tokenId, plaintext);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to create token: name={Name}", name);
            throw;
        }
    }

    /// <summary>
    /// Validates a plaintext token against stored hashes.
    /// Returns the token id if valid, null if not.
    /// </summary>
    public async Task<string?> ValidateTokenAsync(
        string plaintextToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(plaintextToken))
        {
            return null;
        }

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(plaintextToken);
        }
        catch (FormatException)
        {
            _logger.LogDebug("Token validation: valid={IsValid}", false);
            return null;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var tokens = await db.AccessTokens
                .AsNoTracking()
                .Where(t => t.RevokedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var token in tokens)
            {
                var salt = Convert.FromBase64String(token.Salt);
                var storedHash = Convert.FromBase64String(token.HashedToken);
                var computedHash = HashToken(tokenBytes, salt);

                if (CryptographicOperations.FixedTimeEquals(storedHash, computedHash))
                {
                    await UpdateLastUsedAsync(token.Id, ct).ConfigureAwait(false);
                    _logger.LogDebug("Token validation: valid={IsValid}", true);
                    return token.Id;
                }
            }

            _logger.LogDebug("Token validation: valid={IsValid}", false);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to validate token");
            throw;
        }
    }

    /// <summary>Revokes a token by id.</summary>
    public async Task RevokeTokenAsync(
        string tokenId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(tokenId))
        {
            throw new ArgumentException("Token id is required.", nameof(tokenId));
        }

        try
        {
            await _writeQueue.EnqueueAndWaitAsync(async db =>
            {
                var entity = await db.AccessTokens
                    .FindAsync([tokenId], ct)
                    .ConfigureAwait(false);

                if (entity is not null && entity.RevokedAt is null)
                {
                    entity.RevokedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct).ConfigureAwait(false);
                }
            }, ct).ConfigureAwait(false);

            _logger.LogInformation("Token revoked: {TokenId}", tokenId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to revoke token: {TokenId}", tokenId);
            throw;
        }
    }

    /// <summary>
    /// Lists all tokens as metadata. Never returns plaintext or hash values.
    /// </summary>
    public async Task<IReadOnlyList<TokenInfo>> ListTokensAsync(
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await db.AccessTokens
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.ConvertAll(e => new TokenInfo(
            Id: e.Id,
            Name: e.Name,
            CreatedAt: e.CreatedAt,
            LastUsedAt: e.LastUsedAt,
            IsRevoked: e.RevokedAt is not null));
    }

    private async Task UpdateLastUsedAsync(string tokenId, CancellationToken ct)
    {
        await _writeQueue.EnqueueAsync(async db =>
        {
            var entity = await db.AccessTokens
                .FindAsync([tokenId], ct)
                .ConfigureAwait(false);

            if (entity is not null)
            {
                entity.LastUsedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }, ct).ConfigureAwait(false);
    }

    private static byte[] HashToken(byte[] tokenBytes, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            tokenBytes,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            HashSizeBytes);
    }
}
