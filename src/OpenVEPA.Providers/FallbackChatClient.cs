using System.Runtime.CompilerServices;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace OpenVEPA.Providers;

/// <summary>
/// An <see cref="IChatClient"/> that delegates to a primary client and falls back
/// to a secondary client when the primary throws a non-cancellation exception.
/// </summary>
public sealed class FallbackChatClient : IChatClient
{
    private readonly IChatClient _primary;
    private readonly IChatClient _fallback;
    private readonly ILogger<FallbackChatClient>? _logger;

    /// <summary>
    /// Creates a fallback wrapper around two chat clients.
    /// </summary>
    /// <param name="primary">The preferred chat client.</param>
    /// <param name="fallback">The fallback chat client used when <paramref name="primary"/> fails.</param>
    /// <param name="logger">Optional logger for fallback events.</param>
    public FallbackChatClient(
        IChatClient primary,
        IChatClient fallback,
        ILogger<FallbackChatClient>? logger = null)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.GetResponseAsync(chatMessages, options, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogWarning(ex, "Primary chat client failed. Falling back to secondary provider.");
            return await _fallback.GetResponseAsync(chatMessages, options, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Determine which stream to use by probing the primary.
        // If the first MoveNextAsync fails, switch to the fallback.
        var source = await ResolveStreamAsync(chatMessages, options, cancellationToken);

        await foreach (var update in source.WithCancellation(cancellationToken))
        {
            yield return update;
        }
    }

    private async Task<IAsyncEnumerable<ChatResponseUpdate>> ResolveStreamAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        try
        {
            var primaryStream = _primary.GetStreamingResponseAsync(chatMessages, options, cancellationToken);

            // Probe the first element to verify the primary connection works.
            var enumerator = primaryStream.GetAsyncEnumerator(cancellationToken);
            try
            {
                var hasFirst = await enumerator.MoveNextAsync();
                if (!hasFirst)
                {
                    await enumerator.DisposeAsync();
                    return AsyncEnumerable.Empty<ChatResponseUpdate>();
                }

                // Primary works. Return a wrapper that yields the first element, then the rest.
                return PrependAndContinue(enumerator.Current, enumerator);
            }
            catch
            {
                await enumerator.DisposeAsync();
                throw;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogWarning(ex, "Primary streaming client failed. Falling back to secondary provider.");
            return _fallback.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> PrependAndContinue(
        ChatResponseUpdate first,
        IAsyncEnumerator<ChatResponseUpdate> enumerator)
    {
        try
        {
            yield return first;

            while (await enumerator.MoveNextAsync())
            {
                yield return enumerator.Current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return _primary.GetService(serviceType, serviceKey)
            ?? _fallback.GetService(serviceType, serviceKey);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _primary.Dispose();
        _fallback.Dispose();
    }
}
