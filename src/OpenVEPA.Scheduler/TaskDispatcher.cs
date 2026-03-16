using Hangfire;
using Microsoft.Extensions.Logging;

namespace OpenVEPA.Scheduler;

/// <summary>
/// Dispatches one-off tasks through Hangfire for durable background execution.
/// Returns a job ID that can be used to track the task.
/// </summary>
public sealed class TaskDispatcher
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<TaskDispatcher> _logger;

    /// <summary>Initializes a new instance of the <see cref="TaskDispatcher"/> class.</summary>
    public TaskDispatcher(
        IBackgroundJobClient backgroundJobClient,
        ILogger<TaskDispatcher> logger)
    {
        _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Enqueues a one-off background job to execute the given prompt.
    /// </summary>
    /// <param name="prompt">The prompt to process.</param>
    /// <param name="agentName">Optional agent name to route to.</param>
    /// <returns>The Hangfire job ID for tracking.</returns>
    public Task<string> DispatchAsync(string prompt, string? agentName)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(prompt));
        }

        var taskId = $"dispatch-{Guid.NewGuid():N}";

        var jobId = _backgroundJobClient.Enqueue<ScheduledTaskJob>(
            job => job.ExecuteAsync(taskId, prompt, agentName));

        _logger.LogInformation(
            "Dispatched one-off task {TaskId} as Hangfire job {JobId}",
            taskId,
            jobId);

        return Task.FromResult(jobId);
    }
}
