using System;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// Storage interface for job options. Provides methods to store, retrieve, and manage options associated with jobs.
/// </summary>
public interface IJobOptionStorage
{
    /// <summary>
    /// Clears all options associated with the specified job option. This can be used to remove all options for a particular job or option type.
    /// </summary>
    /// <param name="option">The job option whose associated options should be cleared.</param>
    void Clear(IJobOption option);

    /// <summary>
    /// Retrieves the option associated with the specified job option. If the option does not exist, it returns null. This method allows you to get the current option for a job and option type combination.
    /// </summary>
    /// <param name="job">The job option to retrieve.</param>
    /// <returns>The job option if found; otherwise, null.</returns>
    IEnumerable<IJobOption> Get(IJob job);

    /// <summary>
    /// Retrieves the option associated with the specified job and option type. If the option does not exist, it returns null. This method allows you to get the current option for a specific job and option type combination.
    /// </summary>
    /// <param name="job">The job option to retrieve.</param>
    /// <param name="optionType">The type of the option to retrieve.</param>
    /// <returns>A collection of job options that match the specified job and option type.</returns>
    IEnumerable<IJobOption> Get(IJob job, Type optionType);

    /// <summary>
    /// Retrieves all options stored in the storage. This method returns a read-only list of all job options currently stored, allowing you to access and manage all options across different jobs and option types.
    /// </summary>
    /// <returns>A read-only list of all job options currently stored.</returns>
    IReadOnlyList<IJobOption> GetAll();

    /// <summary>
    /// Retrieves all unique job identifiers (owner IDs) that have options stored in the storage. This method returns a collection of job identifiers for which options are currently stored, allowing you to identify which jobs have associated options in the storage.
    /// </summary>
    /// <returns>A collection of job identifiers for which options are currently stored.</returns>
    IEnumerable<IJob> GetOwnerIds();

    /// <summary>
    /// Removes the option associated with the specified job option. This method deletes the option from the storage, effectively disassociating it from the job and option type. It returns true if the option was successfully removed; otherwise, false if the option was not found in the storage.
    /// </summary>
    /// <param name="option">The job option to remove.</param>
    /// <returns>True if the option was successfully removed; otherwise, false.</returns>
    bool Remove(IJobOption option);

    /// <summary>
    /// Sets the option for the specified job option. This method adds or updates the option in the storage, associating it with the job and option type specified in the input. If an option already exists for the same job and option type, it will be overwritten with the new option provided. This allows you to manage and update options for jobs effectively.
    /// </summary>
    /// <param name="option">The job option to set.</param>
    void Set(IJobOption option);

    /// <summary>
    /// Tries to add the specified job option to the storage. This method attempts to add the option without overwriting any existing option for the same job and option type. If an option already exists, it will not be added, and the method will return false. If the option is successfully added, it returns true. This allows you to ensure that options are only added if they do not already exist in the storage.
    /// </summary>
    /// <param name="option">The job option to add.</param>
    /// <returns>True if the option was successfully added; otherwise, false.</returns>
    bool TryAdd(IJobOption option);
}
