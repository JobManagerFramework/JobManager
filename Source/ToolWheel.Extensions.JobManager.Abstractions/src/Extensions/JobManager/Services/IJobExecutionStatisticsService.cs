using System;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Provides access to aggregated execution statistics for registered jobs.
/// </summary>
public interface IJobExecutionStatisticsService
{
    /// <summary>
    /// Returns the execution statistics for a specific job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job.</param>
    /// <returns>The <see cref="JobExecutionStatistics"/> for the job, or <c>null</c> if no data has been recorded.</returns>
    JobExecutionStatistics? GetStatistics(string jobId);

    /// <summary>
    /// Returns execution statistics for all jobs that have recorded at least one execution.
    /// </summary>
    /// <returns>An enumerable of <see cref="JobExecutionStatistics"/> instances.</returns>
    IEnumerable<JobExecutionStatistics> GetAllStatistics();

    /// <summary>
    /// Records a completed execution for the specified job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job.</param>
    /// <param name="jobName">The display name of the job.</param>
    /// <param name="duration">The elapsed execution time.</param>
    /// <param name="status">The final status of the execution.</param>
    void Record(string jobId, string jobName, TimeSpan duration, JobTaskStatusEnum status);

    /// <summary>
    /// Resets the statistics for a specific job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job.</param>
    void Reset(string jobId);

    /// <summary>
    /// Resets the statistics for all jobs.
    /// </summary>
    void ResetAll();
}
