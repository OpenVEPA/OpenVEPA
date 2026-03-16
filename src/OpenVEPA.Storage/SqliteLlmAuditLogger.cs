using Microsoft.Extensions.Logging;
using OpenVEPA.Core.Audit;
using OpenVEPA.Storage.Entities;

namespace OpenVEPA.Storage;

/// <summary>
/// Logs LLM audit entries to the SQLite database via the write queue.
/// </summary>
public sealed class SqliteLlmAuditLogger
{
    private readonly DatabaseWriteQueue _writeQueue;
    private readonly ILogger<SqliteLlmAuditLogger> _logger;

    public SqliteLlmAuditLogger(
        DatabaseWriteQueue writeQueue,
        ILogger<SqliteLlmAuditLogger> logger)
    {
        _writeQueue = writeQueue ?? throw new ArgumentNullException(nameof(writeQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Logs an LLM audit entry to the database.</summary>
    public async Task LogAsync(
        LlmAuditLogEntry entry,
        CancellationToken ct = default)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        await _writeQueue.EnqueueAsync(async db =>
        {
            db.LlmAuditLogEntries.Add(ToEntity(entry));
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        _logger.LogDebug(
            "Logged LLM audit: {Provider}/{Model} {Tokens}in/{Tokens}out",
            entry.Provider,
            entry.Model,
            entry.InputTokens,
            entry.OutputTokens);
    }

    private static LlmAuditLogEntity ToEntity(LlmAuditLogEntry entry)
    {
        return new LlmAuditLogEntity
        {
            Id = entry.Id,
            Timestamp = entry.Timestamp,
            Provider = entry.Provider,
            Model = entry.Model,
            InputTokens = entry.InputTokens,
            OutputTokens = entry.OutputTokens,
            EstimatedCostUsd = entry.EstimatedCostUsd,
            LatencyMs = entry.LatencyMs,
            SessionId = entry.SessionId,
            TaskId = entry.TaskId,
            SkillName = entry.SkillName,
            Success = entry.Success,
            ErrorMessage = entry.ErrorMessage
        };
    }
}
