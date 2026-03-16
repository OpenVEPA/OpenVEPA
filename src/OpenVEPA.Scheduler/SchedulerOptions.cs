namespace OpenVEPA.Scheduler;

/// <summary>Configuration options for the OpenVEPA task scheduler.</summary>
public sealed class SchedulerOptions
{
    /// <summary>Whether the scheduler is enabled. Defaults to true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether the Hangfire dashboard is enabled. Defaults to false.</summary>
    public bool DashboardEnabled { get; set; }

    /// <summary>URL path for the Hangfire dashboard. Defaults to "/hangfire".</summary>
    public string? DashboardPath { get; set; } = "/hangfire";
}
