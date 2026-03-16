using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using OpenVEPA.Storage;

namespace OpenVEPA.Storage.Tests;

/// <summary>
/// Provides a shared in-memory SQLite database with <see cref="OpenVepaDbContext"/>
/// and a running <see cref="DatabaseWriteQueue"/> for integration tests.
/// Each test class gets its own isolated database via a kept-open connection.
/// </summary>
public sealed class TestDbFixture : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _serviceProvider = null!;

    public IDbContextFactory<OpenVepaDbContext> DbFactory { get; private set; } = null!;
    public DatabaseWriteQueue WriteQueue { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddDbContextFactory<OpenVepaDbContext>(options =>
        {
            options.UseSqlite(_connection);
        });

        services.AddDbContext<OpenVepaDbContext>(options =>
        {
            options.UseSqlite(_connection);
        }, ServiceLifetime.Scoped);

        services.AddSingleton<DatabaseWriteQueue>(sp =>
            new DatabaseWriteQueue(sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<DatabaseWriteQueue>.Instance));

        _serviceProvider = services.BuildServiceProvider();

        DbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<OpenVepaDbContext>>();

        // Create the schema.
        await using var db = await DbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        // Start the write queue background service.
        WriteQueue = _serviceProvider.GetRequiredService<DatabaseWriteQueue>();
        await WriteQueue.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await WriteQueue.StopAsync(CancellationToken.None);
        await _serviceProvider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
