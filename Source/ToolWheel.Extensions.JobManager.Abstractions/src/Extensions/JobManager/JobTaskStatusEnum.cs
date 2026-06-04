namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents the execution status of a job task.
/// </summary>
public enum JobTaskStatusEnum
{
    /// <summary>
    /// The task is prepared and waiting to be executed.
    /// </summary>
    Pending,

    /// <summary>
    /// The task is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// The task has completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The task was canceled before completion.
    /// </summary>
    Canceled,

    /// <summary>
    /// The task encountered an error and did not complete successfully.
    /// </summary>
    Failed,

    /// <summary>
    /// The task could not be started because a pre-execution condition evaluated to not ready.
    /// </summary>
    NotReady
}
