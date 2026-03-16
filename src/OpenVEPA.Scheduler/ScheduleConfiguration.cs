namespace OpenVEPA.Scheduler;

/// <summary>Root configuration model for schedules.json binding.</summary>
public sealed class ScheduleConfiguration
{
    /// <summary>List of scheduled tasks defined in configuration.</summary>
    public List<ScheduledTask> Schedules { get; set; } = [];
}
