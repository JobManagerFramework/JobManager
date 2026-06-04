using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service responsible for executing jobs and producing runtime job task handles.
/// </summary>
public interface IJobTaskExecutionService
{
    /// <summary>
    /// Executes the specified <paramref name="job"/> and returns a handle representing the started execution.
    /// Execution conditions are evaluated asynchronously before the job is dispatched.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the <see cref="IJobTask"/> that represents the started execution.</returns>
    ValueTask<IJobTask> ExecuteAsync(IJob job, CancellationToken cancellationToken = default);
}
