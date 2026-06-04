using System;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents aggregated execution statistics for a single job.
/// </summary>
public sealed class JobExecutionStatistics
{
    /// <summary>
    /// The unique identifier of the job.
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// The display name of the job.
    /// </summary>
    public required string JobName { get; init; }

    /// <summary>
    /// The total number of completed executions (success, failed, canceled, not-ready).
    /// </summary>
    public int TotalExecutions { get; init; }

    /// <summary>
    /// The number of executions that completed successfully.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// The number of executions that failed with an error.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// The number of executions that were canceled.
    /// </summary>
    public int CanceledCount { get; init; }

    /// <summary>
    /// The number of executions that were skipped because a condition was not ready.
    /// </summary>
    public int NotReadyCount { get; init; }

    /// <summary>
    /// The error rate as a value between 0.0 and 1.0 (failed / total).
    /// Returns 0.0 when no executions have been recorded.
    /// </summary>
    public double ErrorRate { get; init; }

    /// <summary>
    /// The average execution time across all completed executions.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; init; }

    /// <summary>
    /// The shortest recorded execution time, or <c>null</c> if no executions exist.
    /// </summary>
    public TimeSpan? MinExecutionTime { get; init; }

    /// <summary>
    /// The longest recorded execution time, or <c>null</c> if no executions exist.
    /// </summary>
    public TimeSpan? MaxExecutionTime { get; init; }

    /// <summary>
    /// The timestamp of the most recent execution, or <c>null</c> if no executions exist.
    /// </summary>
    public DateTime? LastExecutionTimestamp { get; init; }
}
