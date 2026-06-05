using System;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service for storing and retrieving job options per job.
/// Options are keyed by job and option type so that each job holds at most one instance per option type.
/// </summary>
public interface IJobOptionService
{
    /// <summary>
    /// Adds or updates the job option for the specified job using the option's concrete runtime type as the key.
    /// </summary>
    /// <param name="job">The job to configure. Must not be <c>null</c>.</param>
    /// <param name="option">The job option instance to store. Must not be <c>null</c>.</param>
    void Add(IJob job, object option);

    /// <summary>
    /// Adds the job option for the specified job. If an option of the same type already exists it is replaced.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    /// <param name="job">The job to configure. Must not be <c>null</c>.</param>
    /// <param name="option">The option instance to store. Must not be <c>null</c>.</param>
    void Add<T>(IJob job, T option) where T : notnull;

    /// <summary>
    /// Updates an existing job option for the specified job.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    /// <param name="job">The job whose option should be updated. Must not be <c>null</c>.</param>
    /// <param name="option">The updated option instance. Must not be <c>null</c>.</param>
    /// <returns><c>true</c> if the option was updated; <c>false</c> if no option of type <typeparamref name="T"/> exists for the job.</returns>
    bool Update<T>(IJob job, T option) where T : notnull;

    /// <summary>
    /// Removes the option of type <typeparamref name="T"/> for the specified job.
    /// </summary>
    /// <typeparam name="T">The type of the option to remove.</typeparam>
    /// <param name="job">The job whose option should be removed. Must not be <c>null</c>.</param>
    /// <returns><c>true</c> if the option was removed; <c>false</c> if it did not exist.</returns>
    bool Remove<T>(IJob job) where T : notnull;

    /// <summary>
    /// Returns the option of type <typeparamref name="T"/> for the specified job, or <c>null</c> if not configured.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    /// <param name="job">The job to query. Must not be <c>null</c>.</param>
    /// <returns>The option instance if found; otherwise <c>null</c>.</returns>
    T? Get<T>(IJob job) where T : class;

    /// <summary>
    /// Returns <c>true</c> if an option of type <typeparamref name="T"/> is configured for the specified job.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    /// <param name="job">The job to check. Must not be <c>null</c>.</param>
    bool Contains<T>(IJob job) where T : notnull;

    /// <summary>
    /// Returns all job options configured for the specified job.
    /// </summary>
    /// <param name="job">The job to query. Must not be <c>null</c>.</param>
    /// <returns>A read-only list of all options stored for the job.</returns>
    IReadOnlyList<IJobOption> GetAll(IJob job);
}
