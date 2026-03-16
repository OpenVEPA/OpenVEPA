using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenVEPA.Storage.Tests;

public sealed class DatabaseWriteQueueTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _serviceProvider = null!;
    private DatabaseWriteQueue _queue = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<OpenVepaDbContext>(options =>
        {
            options.UseSqlite(_connection);
        }, ServiceLifetime.Scoped);

        _serviceProvider = services.BuildServiceProvider();

        // Create schema.
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenVepaDbContext>();
        await db.Database.EnsureCreatedAsync();

        _queue = new DatabaseWriteQueue(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DatabaseWriteQueue>.Instance);

        await _queue.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await _queue.StopAsync(CancellationToken.None);
        await _serviceProvider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task EnqueueAndWaitAsync_ExecutesCallback()
    {
        var executed = false;

        await _queue.EnqueueAndWaitAsync(async db =>
        {
            executed = true;
            await Task.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task EnqueueAndWaitAsync_WithResult_ReturnsValue()
    {
        var result = await _queue.EnqueueAndWaitAsync(async db =>
        {
            await Task.CompletedTask;
            return 42;
        });

        result.Should().Be(42);
    }

    [Fact]
    public async Task EnqueueAndWaitAsync_WithException_Propagates()
    {
        var act = async () =>
        {
            await _queue.EnqueueAndWaitAsync(async db =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("test error");
            });
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("test error")
            ;
    }

    [Fact]
    public async Task EnqueueAndWaitAsync_ProvidesDbContext()
    {
        var contextProvided = false;

        await _queue.EnqueueAndWaitAsync(async db =>
        {
            contextProvided = db is not null;
            await Task.CompletedTask;
        });

        contextProvided.Should().BeTrue();
    }

    [Fact]
    public async Task EnqueueAndWaitAsync_MultipleOperations_ExecuteSequentially()
    {
        var order = new List<int>();

        await _queue.EnqueueAndWaitAsync(async db =>
        {
            order.Add(1);
            await Task.CompletedTask;
        });

        await _queue.EnqueueAndWaitAsync(async db =>
        {
            order.Add(2);
            await Task.CompletedTask;
        });

        await _queue.EnqueueAndWaitAsync(async db =>
        {
            order.Add(3);
            await Task.CompletedTask;
        });

        order.Should().ContainInOrder(1, 2, 3);
    }
}
