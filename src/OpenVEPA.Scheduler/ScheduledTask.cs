namespace OpenVEPA.Scheduler;

/// <summary>Defines a recurring task to execute on a cron schedule.</summary>
public sealed class ScheduledTask
{
    /// <summary>Unique identifier for this scheduled task.</summary>
    public string Id { get; set; } = "";

    /// <summary>Human-readable name for the task.</summary>
    public string Name { get; set; } = "";

    /// <summary>Cron expression defining the schedule (e.g. "0 * * * *" for hourly).</summary>
    public string CronExpression { get; set; } = "";

    /// <summary>The prompt to send to the agent when the task fires.</summary>
    public string Prompt { get; set; } = "";

    /// <summary>Optional agent name to route the task to. Null uses the default agent.</summary>
    public string? AgentName { get; set; }

    /// <summary>Whether this scheduled task is active. Defaults to true.</summary>
    public bool Enabled { get; set; } = true;
}
