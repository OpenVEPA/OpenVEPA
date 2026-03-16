using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenVEPA.Storage;

/// <summary>
/// Single-writer queue that serializes all database write operations
/// through a bounded channel processed by a background hosted service.
/// </summary>
public sealed class DatabaseWriteQueue : BackgroundService
{
    private readonly Channel<Func<OpenVepaDbContext, Task>> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseWriteQueue> _logger;

    public DatabaseWriteQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseWriteQueue> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channel = Channel.CreateBounded<Func<OpenVepaDbContext, Task>>(
            new BoundedChannelOptions(capacity: 1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    }

    /// <summary>Enqueues a write operation for sequential execution.</summary>
    public async Task EnqueueAsync(
        Func<OpenVepaDbContext, Task> writeOperation,
        CancellationToken ct = default)
    {
        if (writeOperation == null)
        {
            throw new ArgumentNullException(nameof(writeOperation));
        }

        await _channel.Writer.WriteAsync(writeOperation, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Enqueues a write operation and waits for it to complete.
    /// Returns the result produced by the operation.
    /// </summary>
    public async Task<T> EnqueueAndWaitAsync<T>(
        Func<OpenVepaDbContext, Task<T>> writeOperation,
        CancellationToken ct = default)
    {
        if (writeOperation == null)
        {
            throw new ArgumentNullException(nameof(writeOperation));
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        await _channel.Writer.WriteAsync(async db =>
        {
            try
            {
                var result = await writeOperation(db).ConfigureAwait(false);
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, ct).ConfigureAwait(false);

        return await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Enqueues a write operation and waits for it to complete (void variant).
    /// </summary>
    public async Task EnqueueAndWaitAsync(
        Func<OpenVepaDbContext, Task> writeOperation,
        CancellationToken ct = default)
    {
        if (writeOperation == null)
        {
            throw new ArgumentNullException(nameof(writeOperation));
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await _channel.Writer.WriteAsync(async db =>
        {
            try
            {
                await writeOperation(db).ConfigureAwait(false);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, ct).ConfigureAwait(false);

        await tcs.Task.ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DatabaseWriteQueue started");

        await foreach (var operation in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<OpenVepaDbContext>();
                await operation(db).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing database write operation");
            }
        }

        _logger.LogInformation("DatabaseWriteQueue stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
