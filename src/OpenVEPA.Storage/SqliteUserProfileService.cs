using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenVEPA.Core.Preferences;
using OpenVEPA.Storage.Entities;

namespace OpenVEPA.Storage;

/// <summary>
/// SQLite-backed implementation of <see cref="IUserProfileService"/>.
/// Reads go directly to the database. Writes are serialized through
/// the <see cref="DatabaseWriteQueue"/>.
/// </summary>
public sealed class SqliteUserProfileService : IUserProfileService
{
    private readonly DatabaseWriteQueue _writeQueue;
    private readonly IDbContextFactory<OpenVepaDbContext> _dbFactory;
    private readonly ILogger<SqliteUserProfileService> _logger;

    public SqliteUserProfileService(
        DatabaseWriteQueue writeQueue,
        IDbContextFactory<OpenVepaDbContext> dbFactory,
        ILogger<SqliteUserProfileService> logger)
    {
        _writeQueue = writeQueue ?? throw new ArgumentNullException(nameof(writeQueue));
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserPreferences> GetRelevantPreferencesAsync(
        string? taskDomain,
        CancellationToken ct)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

            IQueryable<UserPreferenceEntity> query = db.UserPreferenceEntries.AsNoTracking();

            if (!string.IsNullOrEmpty(taskDomain))
            {
                query = query.Where(p =>
                    p.Category == taskDomain ||
                    p.Category == "general");
            }

            var entities = await query.ToListAsync(ct).ConfigureAwait(false);

            var entries = entities
                .Select(ToDomain)
                .ToDictionary(e => e.Key, e => e);

            _logger.LogDebug(
                "Loading preferences for domain={TaskDomain}, found={Count}",
                taskDomain,
                entries.Count);

            return new UserPreferences(entries);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load preferences for domain={TaskDomain}", taskDomain);
            throw;
        }
    }

    public async Task SetExplicitPreferenceAsync(
        string key,
        string value,
        string category,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Value is required.", nameof(value));
        }

        if (string.IsNullOrEmpty(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        try
        {
            await _writeQueue.EnqueueAndWaitAsync(async db =>
            {
                var existing = await db.UserPreferenceEntries
                    .FindAsync([key], ct)
                    .ConfigureAwait(false);

                var now = DateTime.UtcNow;

                if (existing is not null)
                {
                    existing.Value = value;
                    existing.Category = category;
                    existing.Source = PreferenceSource.Explicit.ToString();
                    existing.Confidence = 1.0;
                    existing.UpdatedAt = now;
                }
                else
                {
                    db.UserPreferenceEntries.Add(new UserPreferenceEntity
                    {
                        Id = key,
                        Value = value,
                        Category = category,
                        Source = PreferenceSource.Explicit.ToString(),
                        Confidence = 1.0,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            _logger.LogDebug("Preference saved: key={Key}, category={Category}", key, category);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to save preference: key={Key}, category={Category}", key, category);
            throw;
        }
    }

    public async Task DeletePreferenceAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        await _writeQueue.EnqueueAndWaitAsync(async db =>
        {
            var entity = await db.UserPreferenceEntries
                .FindAsync([key], ct)
                .ConfigureAwait(false);

            if (entity is not null)
            {
                db.UserPreferenceEntries.Remove(entity);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PreferenceEntry>> GetAllPreferencesAsync(
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entities = await db.UserPreferenceEntries
            .AsNoTracking()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.ConvertAll(ToDomain);
    }

    private static PreferenceEntry ToDomain(UserPreferenceEntity entity)
    {
        return new PreferenceEntry(
            Key: entity.Id,
            Value: entity.Value,
            Category: entity.Category,
            Source: Enum.Parse<PreferenceSource>(entity.Source),
            Confidence: entity.Confidence,
            UpdatedAt: entity.UpdatedAt);
    }
}
