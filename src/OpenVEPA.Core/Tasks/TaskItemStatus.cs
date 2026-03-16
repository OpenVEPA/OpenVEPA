namespace OpenVEPA.Core.Tasks;

/// <summary>Tracks the lifecycle state of a queued task.</summary>
public enum TaskItemStatus
{
    /// <summary>Task is waiting to be executed.</summary>
    Pending,

    /// <summary>Task is currently executing.</summary>
    Running,

    /// <summary>Task completed successfully.</summary>
    Completed,

    /// <summary>Task terminated with an error.</summary>
    Failed,

    /// <summary>Task was cancelled before completion.</summary>
    Cancelled
}
