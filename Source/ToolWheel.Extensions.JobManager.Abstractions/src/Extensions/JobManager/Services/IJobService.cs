using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToolWheel.Extensions.JobManager.Configuration;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service responsible for registering, querying and executing jobs.
/// Implementations manage the lifetime and execution of <see cref="IJob"/> instances and produce corresponding <see cref="IJobTask"/> objects.
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Adds a new job to the job store based on the provided <paramref name="jobDescription"/>.
    /// </summary>
    /// <param name="jobDescription">The description that describes the job to add.</param>
    /// <returns>The created <see cref="IJob"/> instance representing the added job.</returns>
    IJob Add(IJobDescription jobDescription);

    /// <summary>
    /// Attempts to find a job by its identifier.
    /// </summary>
    /// <param name="jobId">The identifier of the job to locate.</param>
    /// <returns>The matching <see cref="IJob"/> if found; otherwise <c>null</c>.</returns>
    IJob? FindById(string jobId);

    /// <summary>
    /// Reads a job by its identifier.
    /// </summary>
    /// <param name="jobId">The identifier of the job to read.</param>
    /// <returns>The matching <see cref="IJob"/>.</returns>
    /// <remarks>
    /// Implementations may throw if the job cannot be found; callers should handle exceptions accordingly.
    /// </remarks>
    IJob ReadById(string jobId);

    /// <summary>
    /// Returns all registered jobs.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{IJob}"/> that enumerates all known jobs.</returns>
    IEnumerable<IJob> ReadAll();

    /// <summary>
    /// Removes the specified job from the job store.
    /// </summary>
    /// <param name="job">The job instance to remove.</param>
    /// <returns><c>true</c> if the job was successfully removed; otherwise <c>false</c>.</returns>
    bool Remove(IJob job);

    /// <summary>
    /// Executes the given job and returns a runtime handle representing the execution (<see cref="IJobTask"/>).
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the <see cref="IJobTask"/> that represents the started execution.</returns>
    ValueTask<IJobTask> ExecuteAsync(IJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing job in the job store with the provided <paramref name="updateJob"/> instance.
    /// </summary>
    /// <param name="updateJob">The updated job instance. The <see cref="IJob.Id"/> must match an existing job.</param>
    /// <exception cref="System.InvalidOperationException">Thrown when no job with the given id exists.</exception>
    void Update(IJob updateJob);

    /// <summary>
    /// Updates name, description and enabled state of the job with the given <paramref name="jobId"/>.
    /// </summary>
    /// <param name="jobId">The identifier of the job to update.</param>
    /// <param name="name">The new display name.</param>
    /// <param name="description">The new description.</param>
    /// <param name="enabled">Whether the job should be enabled.</param>
    /// <exception cref="System.InvalidOperationException">Thrown when no job with the given id exists.</exception>
    void Update(string jobId, string name, string description, bool enabled);
}
