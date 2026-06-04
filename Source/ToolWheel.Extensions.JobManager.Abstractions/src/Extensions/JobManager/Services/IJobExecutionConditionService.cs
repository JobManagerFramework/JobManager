using System.Collections.Generic;
using ToolWheel.Extensions.JobManager.Configuration;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service for storing and retrieving execution condition features per job.
/// Features are keyed by job and feature type so that each job holds at most one instance per feature type.
/// Third-party libraries can store any <see cref="IJobManagerFeature"/> implementation without changes to this service.
/// </summary>
public interface IJobExecutionConditionService
{
    /// <summary>
    /// Adds or updates the feature for the specified job using the feature's concrete runtime type as the key.
    /// Convenience overload for use in default interface implementations where the generic type is unavailable.
    /// </summary>
    /// <param name="job">The job to configure. Must not be <c>null</c>.</param>
    /// <param name="feature">The feature instance to store. Must not be <c>null</c>.</param>
    void Add(IJob job, IJobManagerFeature feature);

    /// <summary>
    /// Adds the feature for the specified job. If a feature of the same type already exists it is replaced.
    /// </summary>
    /// <typeparam name="T">The type of the feature.</typeparam>
    /// <param name="job">The job to configure. Must not be <c>null</c>.</param>
    /// <param name="feature">The feature instance to store. Must not be <c>null</c>.</param>
    void Add<T>(IJob job, T feature) where T : IJobManagerFeature;

    /// <summary>
    /// Updates an existing feature for the specified job.
    /// </summary>
    /// <typeparam name="T">The type of the feature.</typeparam>
    /// <param name="job">The job whose feature should be updated. Must not be <c>null</c>.</param>
    /// <param name="feature">The updated feature instance. Must not be <c>null</c>.</param>
    /// <returns><c>true</c> if the feature was updated; <c>false</c> if no feature of type <typeparamref name="T"/> exists for the job.</returns>
    bool Update<T>(IJob job, T feature) where T : IJobManagerFeature;

    /// <summary>
    /// Removes the feature of type <typeparamref name="T"/> for the specified job.
    /// </summary>
    /// <typeparam name="T">The type of the feature to remove.</typeparam>
    /// <param name="job">The job whose feature should be removed. Must not be <c>null</c>.</param>
    /// <returns><c>true</c> if the feature was removed; <c>false</c> if it did not exist.</returns>
    bool Remove<T>(IJob job) where T : IJobManagerFeature;

    /// <summary>
    /// Returns the feature of type <typeparamref name="T"/> for the specified job, or <c>null</c> if not configured.
    /// </summary>
    /// <typeparam name="T">The type of the feature.</typeparam>
    /// <param name="job">The job to query. Must not be <c>null</c>.</param>
    /// <returns>The feature instance if found; otherwise <c>null</c>.</returns>
    T? Get<T>(IJob job) where T : class, IJobManagerFeature;

    /// <summary>
    /// Returns <c>true</c> if a feature of type <typeparamref name="T"/> is configured for the specified job.
    /// </summary>
    /// <typeparam name="T">The type of the feature.</typeparam>
    /// <param name="job">The job to check. Must not be <c>null</c>.</param>
    bool Contains<T>(IJob job) where T : IJobManagerFeature;

    /// <summary>
    /// Returns all features configured for the specified job.
    /// </summary>
    /// <param name="job">The job to query. Must not be <c>null</c>.</param>
    /// <returns>A read-only list of all features stored for the job.</returns>
    IReadOnlyList<IJobManagerFeature> GetAll(IJob job);
}
