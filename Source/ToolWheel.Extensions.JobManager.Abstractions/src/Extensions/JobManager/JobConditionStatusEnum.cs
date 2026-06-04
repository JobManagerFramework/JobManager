namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Specifies the evaluation result of a job's preconditions or execution readiness.
/// </summary>
public enum JobConditionStatusEnum
{
    /// <summary>
    /// The job is ready for execution.
    /// </summary>
    Ready,

    /// <summary>
    /// The job is not ready for execution.
    /// </summary>
    NotReady
}
