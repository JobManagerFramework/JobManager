using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// Provides a storage abstraction for <see cref="JobTaskJournalEntry"/> instances, keyed by job task identifier.
/// Implementations can be swapped to persist journal entries in a database or any other store.
/// </summary>
public interface IJobTaskJournalStorage
{
    /// <summary>
    /// Appends a single journal entry for the specified job task.
    /// </summary>
    /// <param name="jobTaskId">The identifier of the job task.</param>
    /// <param name="entry">The journal entry to append.</param>
    void Append(string jobTaskId, JobTaskJournalEntry entry);

    /// <summary>
    /// Returns all journal entries recorded for the specified job task.
    /// </summary>
    /// <param name="jobTaskId">The identifier of the job task.</param>
    /// <returns>A read-only collection of <see cref="JobTaskJournalEntry"/> instances. Returns an empty collection when no entries are found.</returns>
    IReadOnlyCollection<JobTaskJournalEntry> GetEntries(string jobTaskId);
}
