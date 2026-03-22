using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.AI;

using OpenVEPA.Core.Audit;

namespace OpenVEPA.Providers;

/// <summary>
/// Delegating <see cref="IChatClient"/> that captures token usage, measures latency,
/// and creates <see cref="LlmAuditLogEntry"/> records for every call.
/// Decoupled from storage via an optional async callback.
/// </summary>
public sealed class TokenTrackingChatClient : DelegatingChatClient
{
    private readonly string _provider;
    private readonly string _model;
    private readonly Func<LlmAuditLogEntry, Task>? _auditLogger;

    /// <summary>
    /// Creates a new token-tracking wrapper around <paramref name="innerClient"/>.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="provider">Provider name for audit entries (e.g. "openai").</param>
    /// <param name="model">Model name for audit entries (e.g. "gpt-4o").</param>
    /// <param name="auditLogger">Optional callback invoked with each audit entry.</param>
    public TokenTrackingChatClient(
        IChatClient innerClient,
        string provider,
        string model,
        Func<LlmAuditLogEntry, Task>? auditLogger = null)
        : base(innerClient)
    {
        if (innerClient is null) throw new ArgumentNullException(nameof(innerClient));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider name is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("Model name is required.", nameof(model));

        _provider = provider;
        _model = model;
        _auditLogger = auditLogger;
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = true;
        string? errorMessage = null;
        ChatResponse? response = null;
        var actualModel = options?.ModelId ?? _model;

        try
        {
            response = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            success = false;
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await LogAuditEntryAsync(
                actualModel,
                response?.Usage,
                stopwatch.ElapsedMilliseconds,
                success,
                errorMessage).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        UsageDetails? lastUsage = null;
        var actualModel = options?.ModelId ?? _model;

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
        {
            // Usage arrives as UsageContent items within the Contents collection.
            foreach (var content in update.Contents)
            {
                if (content is UsageContent usageContent && usageContent.Details is not null)
                {
                    lastUsage = usageContent.Details;
                }
            }

            yield return update;
        }

        stopwatch.Stop();
        await LogAuditEntryAsync(actualModel, lastUsage, stopwatch.ElapsedMilliseconds, true, null).ConfigureAwait(false);
    }

    private async Task LogAuditEntryAsync(
        string model,
        UsageDetails? usage,
        long latencyMs,
        bool success,
        string? errorMessage)
    {
        if (_auditLogger is null)
        {
            return;
        }

        var entry = new LlmAuditLogEntry(
            Id: Guid.NewGuid().ToString("N"),
            Timestamp: DateTime.UtcNow,
            Provider: _provider,
            Model: model,
            InputTokens: (int)(usage?.InputTokenCount ?? 0),
            OutputTokens: (int)(usage?.OutputTokenCount ?? 0),
            EstimatedCostUsd: null,
            LatencyMs: (int)latencyMs,
            SessionId: null,
            TaskId: null,
            SkillName: null,
            Success: success,
            ErrorMessage: errorMessage);

        await _auditLogger(entry);
    }
}
