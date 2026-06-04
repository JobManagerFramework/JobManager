using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Evaluates all registered execution conditions for a job and returns a single aggregated status.
/// Use this service to determine whether a job is eligible for execution before dispatching it.
/// </summary>
public interface IJobExecutionEvaluatorService
{
    /// <summary>
    /// Evaluates all registered execution conditions asynchronously for the given <paramref name="job"/>.
    /// </summary>
    /// <param name="job">The job to evaluate.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the aggregated <see cref="JobConditionStatus"/>.</returns>
    ValueTask<JobConditionStatus> EvaluateAsync(IJob job, CancellationToken cancellationToken = default);
}
