using System;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Thread-safe accumulator that collects execution data for a single job and produces a snapshot.
/// </summary>
internal sealed class JobStatisticsAccumulator
{
    private readonly object syncRoot = new object();
    private string jobName;
    private int totalExecutions;
    private int successCount;
    private int failedCount;
    private int canceledCount;
    private int notReadyCount;
    private long totalDurationTicks;
    private long minDurationTicks = long.MaxValue;
    private long maxDurationTicks = long.MinValue;
    private DateTime? lastExecution;

    /// <summary>
    /// Initializes a new instance of <see cref="JobStatisticsAccumulator"/> with the initial job name.
    /// </summary>
    /// <param name="jobName">The display name of the job.</param>
    public JobStatisticsAccumulator(string jobName)
    {
        this.jobName = jobName;
    }

    /// <summary>
    /// Records a single execution result.
    /// </summary>
    /// <param name="name">The current display name of the job.</param>
    /// <param name="duration">The elapsed execution time.</param>
    /// <param name="status">The final status of the execution.</param>
    public void Record(string name, TimeSpan duration, JobTaskStatusEnum status)
    {
        lock (syncRoot)
        {
            jobName = name;
            totalExecutions++;

            switch (status)
            {
                case JobTaskStatusEnum.Success:
                    successCount++;
                    break;
                case JobTaskStatusEnum.Failed:
                    failedCount++;
                    break;
                case JobTaskStatusEnum.Canceled:
                    canceledCount++;
                    break;
                case JobTaskStatusEnum.NotReady:
                    notReadyCount++;
                    break;
            }

            long ticks = duration.Ticks;
            totalDurationTicks += ticks;

            if (ticks < minDurationTicks)
            {
                minDurationTicks = ticks;
            }

            if (ticks > maxDurationTicks)
            {
                maxDurationTicks = ticks;
            }

            lastExecution = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Creates a snapshot of the current statistics.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job.</param>
    /// <returns>A <see cref="JobExecutionStatistics"/> instance representing the current state.</returns>
    public JobExecutionStatistics ToStatistics(string jobId)
    {
        lock (syncRoot)
        {
            double errorRate = totalExecutions > 0
                ? (double)failedCount / totalExecutions
                : 0.0;

            TimeSpan average = totalExecutions > 0
                ? TimeSpan.FromTicks(totalDurationTicks / totalExecutions)
                : TimeSpan.Zero;

            return new JobExecutionStatistics
            {
                JobId = jobId,
                JobName = jobName,
                TotalExecutions = totalExecutions,
                SuccessCount = successCount,
                FailedCount = failedCount,
                CanceledCount = canceledCount,
                NotReadyCount = notReadyCount,
                ErrorRate = errorRate,
                AverageExecutionTime = average,
                MinExecutionTime = totalExecutions > 0 ? TimeSpan.FromTicks(minDurationTicks) : null,
                MaxExecutionTime = totalExecutions > 0 ? TimeSpan.FromTicks(maxDurationTicks) : null,
                LastExecutionTimestamp = lastExecution
            };
        }
    }
}
