using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// In-memory implementation of <see cref="IJobExecutionStatisticsService"/>.
/// Collects execution data via <see cref="Record"/> and exposes aggregated statistics.
/// </summary>
public sealed class JobExecutionStatisticsService : IJobExecutionStatisticsService
{
    private readonly ConcurrentDictionary<string, JobStatisticsAccumulator> accumulators = new ConcurrentDictionary<string, JobStatisticsAccumulator>();
    private readonly ILogger<JobExecutionStatisticsService> logger;

    /// <summary>
    /// Initializes a new instance of <see cref="JobExecutionStatisticsService"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public JobExecutionStatisticsService(ILogger<JobExecutionStatisticsService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.logger.LogDebug("JobExecutionStatisticsService created.");
    }

    /// <inheritdoc />
    public void Record(string jobId, string jobName, TimeSpan duration, JobTaskStatusEnum status)
    {
        var accumulator = accumulators.GetOrAdd(jobId, _ => new JobStatisticsAccumulator(jobName));
        accumulator.Record(jobName, duration, status);

        logger.LogDebug(
            "Recorded execution for job {JobId}: Duration={Duration}, Status={Status}",
            jobId, duration, status);
    }

    /// <inheritdoc />
    public JobExecutionStatistics? GetStatistics(string jobId)
    {
        if (accumulators.TryGetValue(jobId, out var accumulator))
        {
            return accumulator.ToStatistics(jobId);
        }

        logger.LogDebug("No statistics found for job {JobId}", jobId);
        return null;
    }

    /// <inheritdoc />
    public IEnumerable<JobExecutionStatistics> GetAllStatistics()
    {
        return accumulators.Select(kvp => kvp.Value.ToStatistics(kvp.Key)).ToArray();
    }

    /// <inheritdoc />
    public void Reset(string jobId)
    {
        if (accumulators.TryRemove(jobId, out _))
        {
            logger.LogInformation("Statistics reset for job {JobId}", jobId);
        }
    }

    /// <inheritdoc />
    public void ResetAll()
    {
        accumulators.Clear();
        logger.LogInformation("All execution statistics have been reset.");
    }
}
