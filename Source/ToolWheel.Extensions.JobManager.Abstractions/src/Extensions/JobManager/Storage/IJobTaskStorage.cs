using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// Provides a storage abstraction for <see cref="IJobTask"/> instances, grouped by their owning <see cref="IJob"/>.
/// Implementations can be swapped to persist job tasks in a database or any other store.
/// </summary>
public interface IJobTaskStorage
{
    /// <summary>
    /// Adds a job task to the storage, associated with the given <paramref name="job"/>.
    /// </summary>
    /// <param name="job">The job that owns the task.</param>
    /// <param name="jobTask">The task to add.</param>
    void Add(IJob job, IJobTask jobTask);

    /// <summary>
    /// Attempts to find a job task by its identifier within the specified job.
    /// </summary>
    /// <param name="job">The job to search within.</param>
    /// <param name="jobTaskId">The identifier of the task to find.</param>
    /// <returns>The matching <see cref="IJobTask"/> if found; otherwise <c>null</c>.</returns>
    IJobTask? FindByTaskId(IJob job, string jobTaskId);

    /// <summary>
    /// Removes the specified job task from the storage.
    /// </summary>
    /// <param name="jobTask">The job task to remove.</param>
    /// <returns><c>true</c> if the task was removed; otherwise <c>false</c>.</returns>
    bool TryRemove(IJobTask jobTask);

    /// <summary>
    /// Returns all job tasks across all jobs.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{IJobTask}"/> of all stored job tasks.</returns>
    IEnumerable<IJobTask> GetAll();

    /// <summary>
    /// Returns all job tasks associated with the specified job.
    /// </summary>
    /// <param name="job">The job whose tasks should be returned.</param>
    /// <returns>An <see cref="IEnumerable{IJobTask}"/> for the given job. Returns an empty collection when no tasks are registered for the job.</returns>
    IEnumerable<IJobTask> GetByJob(IJob job);

    /// <summary>
    /// Returns all job tasks associated with the specified job that match any of the provided statuses.
    /// </summary>
    /// <param name="job">The job whose tasks should be returned.</param>
    /// <param name="statuses">The statuses to filter by. When the collection is empty, all tasks for the job are returned.</param>
    /// <returns>An <see cref="IEnumerable{IJobTask}"/> matching the given statuses.</returns>
    IEnumerable<IJobTask> GetByJob(IJob job, IEnumerable<JobTaskStatusEnum> statuses);
}
