using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ToolWheel.Extensions.JobManager.Services;

namespace ToolWheel.Extensions.JobManager.Middleware;

/// <summary>
/// Execution middleware that measures each job execution and forwards the result
/// to <see cref="IJobExecutionStatisticsService"/> for aggregation.
/// </summary>
public sealed class JobExecutionStatisticsMiddleware : IExecutionMiddleware
{
    private readonly IJobExecutionStatisticsService statisticsService;

    /// <summary>
    /// Initializes a new instance of <see cref="JobExecutionStatisticsMiddleware"/>.
    /// </summary>
    /// <param name="statisticsService">The statistics service that receives execution records.</param>
    public JobExecutionStatisticsMiddleware(IJobExecutionStatisticsService statisticsService)
    {
        this.statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
    }

    /// <inheritdoc />
    public async Task InvokeAsync(IJobTaskContextBuilder context, Func<Task> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        await next().ConfigureAwait(false);

        stopwatch.Stop();

        statisticsService.Record(
            context.Job.Id,
            context.Job.Name,
            stopwatch.Elapsed,
            context.JobTask.Status);
    }
}
