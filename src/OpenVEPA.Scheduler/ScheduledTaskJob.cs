using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenVEPA.Core.Agents;

namespace OpenVEPA.Scheduler;

/// <summary>
/// Hangfire job class that executes a scheduled task by resolving
/// <see cref="IAgentRuntime"/> from DI and processing the prompt.
/// </summary>
public sealed class ScheduledTaskJob
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ScheduledTaskJob> _logger;

    /// <summary>Initializes a new instance of the <see cref="ScheduledTaskJob"/> class.</summary>
    public ScheduledTaskJob(
        IServiceProvider services,
        ILogger<ScheduledTaskJob> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a scheduled task. Called by Hangfire when a job fires.
    /// </summary>
    /// <param name="taskId">Identifier of the scheduled task for logging.</param>
    /// <param name="prompt">The prompt to send to the agent.</param>
    /// <param name="agentName">Optional agent name to route the task to.</param>
    public async Task ExecuteAsync(string taskId, string prompt, string? agentName)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            throw new ArgumentException("Task ID is required.", nameof(taskId));
        }

        if (string.IsNullOrEmpty(prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(prompt));
        }

        _logger.LogInformation(
            "Executing scheduled task {TaskId} with agent {AgentName}",
            taskId,
            agentName ?? "default");

        using var scope = _services.CreateScope();
        var runtime = scope.ServiceProvider.GetService<IAgentRuntime>();

        if (runtime is null)
        {
            _logger.LogWarning(
                "No IAgentRuntime registered. Skipping task {TaskId}.",
                taskId);
            return;
        }

        var sessionId = $"scheduled-{taskId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        try
        {
            var response = await runtime
                .ProcessMessageAsync(sessionId, prompt, CancellationToken.None)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Scheduled task {TaskId} completed. Response length: {Length} chars",
                taskId,
                response.Content.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Scheduled task {TaskId} failed",
                taskId);
            throw;
        }
    }
}
