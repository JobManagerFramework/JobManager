using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service responsible for managing runtime job tasks.
/// Implementations create, track and remove <see cref="IJobTask"/> instances and provide querying capabilities.
/// </summary>
public interface IJobTaskService
{
    /// <summary>
    /// Executes the specified <paramref name="job"/> and returns a handle representing the started execution.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the <see cref="IJobTask"/> that represents the started execution.</returns>
    ValueTask<IJobTask> ExecuteAsync(IJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified job task from the tracking collection or execution queue.
    /// </summary>
    /// <param name="jobTask">The job task to remove.</param>
    void Remove(IJobTask jobTask);

    /// <summary>
    /// Returns all currently known or tracked job tasks.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{IJobTask}"/> that enumerates all job tasks.</returns>
    IEnumerable<IJobTask> ReadAll();

    /// <summary>
    /// Returns all job tasks associated with the specified <paramref name="job"/>.
    /// </summary>
    /// <param name="job">The job whose tasks should be returned.</param>
    /// <returns>An <see cref="IEnumerable{IJobTask}"/> that enumerates the tasks for the given job.</returns>
    IEnumerable<IJobTask> ReadByJob(IJob job);

    /// <summary>
    /// Retrieves all job tasks associated with the specified job that match any of the provided statuses.
    /// </summary>
    /// <param name="job">The job for which to retrieve associated tasks. Cannot be null.</param>
    /// <param name="status">An optional array of task statuses to filter the results. If no statuses are specified, all tasks for the job
    /// are returned.</param>
    /// <returns>An enumerable collection of job tasks that belong to the specified job and match any of the given statuses.
    /// Returns an empty collection if no matching tasks are found.</returns>
    IEnumerable<IJobTask> ReadByJob(IJob job, params JobTaskStatusEnum[] status);

    /// <summary>
    /// Requests cancellation for all currently tracked job tasks and waits until they have completed.
    /// Implementations should signal cancellation on each task's <see cref="IJobTask.CancellationToken"/>
    /// and then await their underlying <see cref="IJobTask.Task"/> instances.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the wait for tasks to complete.</param>
    /// <returns>A task that completes when all tracked job tasks have finished or the <paramref name="cancellationToken"/> is triggered.</returns>
    Task CancelAllAndWaitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to find a specific task by its identifier within the given job.
    /// </summary>
    /// <param name="job">The job that owns the task.</param>
    /// <param name="jobTaskId">The identifier of the task to locate.</param>
    /// <returns>The matching <see cref="IJobTask"/> if found; otherwise <c>null</c>.</returns>
    IJobTask? FindByTaskId(IJob job, string jobTaskId);

    /// <summary>
    /// Requests cancellation for the specified job task by appending a journal entry
    /// and signalling its <see cref="IJobTask.CancellationToken"/>.
    /// The task status will transition to <see cref="JobTaskStatusEnum.Canceled"/> asynchronously.
    /// </summary>
    /// <param name="jobTask">The job task to cancel. Cannot be <c>null</c>.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="jobTask"/> is <c>null</c>.</exception>
    void CancelTask(IJobTask jobTask);
}
