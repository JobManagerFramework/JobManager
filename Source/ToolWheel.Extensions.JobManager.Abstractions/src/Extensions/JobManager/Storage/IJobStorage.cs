using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// Provides a storage abstraction for <see cref="IJob"/> instances.
/// Implementations can be swapped to persist jobs in a database or any other store.
/// </summary>
public interface IJobStorage
{
    /// <summary>
    /// Attempts to add a job to the storage.
    /// </summary>
    /// <param name="job">The job to add.</param>
    /// <returns><c>true</c> if the job was added; <c>false</c> if a job with the same id already exists.</returns>
    bool TryAdd(IJob job);

    /// <summary>
    /// Attempts to find a job by its identifier.
    /// </summary>
    /// <param name="jobId">The identifier of the job to find.</param>
    /// <returns>The matching <see cref="IJob"/> if found; otherwise <c>null</c>.</returns>
    IJob? FindById(string jobId);

    /// <summary>
    /// Atomically replaces <paramref name="originalJob"/> with <paramref name="updatedJob"/>
    /// when the stored value equals <paramref name="originalJob"/>.
    /// </summary>
    /// <param name="jobId">The identifier of the job to update.</param>
    /// <param name="updatedJob">The new job value.</param>
    /// <param name="originalJob">The expected current value used for the compare-and-swap.</param>
    /// <returns><c>true</c> if the update succeeded; otherwise <c>false</c>.</returns>
    bool TryUpdate(string jobId, IJob updatedJob, IJob originalJob);

    /// <summary>
    /// Removes the job with the specified identifier from the storage.
    /// </summary>
    /// <param name="jobId">The identifier of the job to remove.</param>
    /// <returns><c>true</c> if the job was removed; otherwise <c>false</c>.</returns>
    bool TryRemove(string jobId);

    /// <summary>
    /// Returns all jobs currently held in the storage.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{IJob}"/> of all stored jobs.</returns>
    IEnumerable<IJob> GetAll();
}
