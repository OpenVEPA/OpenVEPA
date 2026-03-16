using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenVEPA.Scheduler;

/// <summary>
/// Manages registration of recurring Hangfire jobs from configuration.
/// Reads <see cref="ScheduleConfiguration"/> on startup and registers
/// each enabled schedule as a Hangfire recurring job.
/// </summary>
public sealed class SchedulerService
{
    private readonly IOptions<ScheduleConfiguration> _config;
    private readonly ILogger<SchedulerService> _logger;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly List<ScheduledTask> _activeTasks = [];

    /// <summary>Initializes a new instance of the <see cref="SchedulerService"/> class.</summary>
    public SchedulerService(
        IOptions<ScheduleConfiguration> config,
        IRecurringJobManager recurringJobManager,
        ILogger<SchedulerService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Registers all enabled schedules from configuration as recurring jobs.</summary>
    public void RegisterConfiguredSchedules()
    {
        var schedules = _config.Value.Schedules;

        _logger.LogInformation(
            "Registering {Count} configured schedules",
            schedules.Count);

        foreach (var task in schedules)
        {
            if (task.Enabled)
            {
                AddSchedule(task);
            }
            else
            {
                _logger.LogDebug(
                    "Skipping disabled schedule {TaskId}",
                    task.Id);
            }
        }
    }

    /// <summary>Adds or updates a recurring schedule.</summary>
    /// <param name="task">The scheduled task definition.</param>
    public void AddSchedule(ScheduledTask task)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (string.IsNullOrEmpty(task.Id))
        {
            throw new ArgumentException("Task ID is required.", nameof(task));
        }

        if (string.IsNullOrEmpty(task.CronExpression))
        {
            throw new ArgumentException("Cron expression is required.", nameof(task));
        }

        _recurringJobManager.AddOrUpdate<ScheduledTaskJob>(
            task.Id,
            job => job.ExecuteAsync(task.Id, task.Prompt, task.AgentName),
            task.CronExpression);

        UpdateActiveTask(task);

        _logger.LogInformation(
            "Registered recurring job {TaskId} ({TaskName}) with cron '{Cron}'",
            task.Id,
            task.Name,
            task.CronExpression);
    }

    /// <summary>Removes a recurring schedule by ID.</summary>
    /// <param name="taskId">The task identifier to remove.</param>
    public void RemoveSchedule(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            throw new ArgumentException("Task ID is required.", nameof(taskId));
        }

        _recurringJobManager.RemoveIfExists(taskId);
        _activeTasks.RemoveAll(t => t.Id == taskId);

        _logger.LogInformation("Removed recurring job {TaskId}", taskId);
    }

    /// <summary>Enables a previously disabled schedule.</summary>
    /// <param name="taskId">The task identifier to enable.</param>
    public void EnableSchedule(string taskId)
    {
        var task = FindTask(taskId);
        if (task is null)
        {
            _logger.LogWarning("Cannot enable unknown schedule {TaskId}", taskId);
            return;
        }

        task.Enabled = true;
        AddSchedule(task);
    }

    /// <summary>Disables an active schedule without removing it.</summary>
    /// <param name="taskId">The task identifier to disable.</param>
    public void DisableSchedule(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            throw new ArgumentException("Task ID is required.", nameof(taskId));
        }

        _recurringJobManager.RemoveIfExists(taskId);

        var task = FindTask(taskId);
        if (task is not null)
        {
            task.Enabled = false;
        }

        _logger.LogInformation("Disabled recurring job {TaskId}", taskId);
    }

    /// <summary>Returns a read-only snapshot of all registered schedules.</summary>
    public IReadOnlyList<ScheduledTask> ListSchedules()
    {
        return _activeTasks.AsReadOnly();
    }

    private ScheduledTask? FindTask(string taskId)
    {
        return _activeTasks.Find(t => t.Id == taskId);
    }

    private void UpdateActiveTask(ScheduledTask task)
    {
        _activeTasks.RemoveAll(t => t.Id == task.Id);
        _activeTasks.Add(task);
    }
}
