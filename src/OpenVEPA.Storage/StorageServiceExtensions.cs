using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenVEPA.Core.Preferences;
using OpenVEPA.Core.Sessions;
using OpenVEPA.Core.Storage;

namespace OpenVEPA.Storage;

/// <summary>Extension methods for registering OpenVEPA storage services.</summary>
public static class StorageServiceExtensions
{
    /// <summary>
    /// Registers the OpenVEPA storage services including SQLite DbContext,
    /// file document store, session store, user profile service,
    /// token store, and LLM audit logger.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">
    /// SQLite connection string, e.g. "Data Source=openvpa.db".
    /// </param>
    /// <param name="documentStorePath">
    /// Base file path for the file document store. Defaults to "data/documents".
    /// </param>
    public static IServiceCollection AddOpenVepaStorage(
        this IServiceCollection services,
        string connectionString,
        string documentStorePath = "data/documents")
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException(
                "Connection string is required.",
                nameof(connectionString));
        }

        // Register the DbContext factory with SQLite and WAL pragmas.
        services.AddDbContextFactory<OpenVepaDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqlite =>
            {
                sqlite.CommandTimeout(30);
            });
        });

        // Register DbContext for scoped injection (used by write queue).
        services.AddDbContext<OpenVepaDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqlite =>
            {
                sqlite.CommandTimeout(30);
            });
        }, ServiceLifetime.Scoped);

        // Background write queue (hosted service + singleton).
        services.AddSingleton<DatabaseWriteQueue>();
        services.AddHostedService(sp => sp.GetRequiredService<DatabaseWriteQueue>());

        // Storage implementations.
        services.AddSingleton<IDocumentStore>(
            _ => new FileDocumentStore(documentStorePath));

        services.AddSingleton<SqliteSessionStore>();
        services.AddSingleton<ISessionStore>(sp => sp.GetRequiredService<SqliteSessionStore>());
        services.AddScoped<IUserProfileService, SqliteUserProfileService>();
        services.AddSingleton<SqliteTokenStore>();
        services.AddSingleton<SqliteLlmAuditLogger>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and WAL mode is enabled.
    /// Call during application startup.
    /// </summary>
    public static async Task InitializeStorageAsync(
        this IServiceProvider services,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenVepaDbContext>();
        await db.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);

        // Apply WAL pragmas on the underlying connection.
        var connection = db.Database.GetDbConnection() as SqliteConnection;
        if (connection is not null)
        {
            OpenVepaDbContext.ApplyPragmas(connection);
        }
    }
}
