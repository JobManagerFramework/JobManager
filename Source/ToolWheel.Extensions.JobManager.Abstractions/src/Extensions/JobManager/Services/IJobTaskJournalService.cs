using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service responsible for recording and querying journal entries produced during job task executions.
/// All log entries written during a job execution are routed through this service.
/// </summary>
public interface IJobTaskJournalService
{
    /// <summary>
    /// Appends a single journal entry for the specified job task.
    /// </summary>
    /// <param name="jobTask">The job task the entry belongs to.</param>
    /// <param name="entry">The journal entry to append.</param>
    void Append(IJobTask jobTask, JobTaskJournalEntry entry);

    /// <summary>
    /// Returns all journal entries recorded for the specified job task.
    /// </summary>
    /// <param name="jobTask">The job task whose entries should be returned.</param>
    /// <returns>A read-only collection of <see cref="JobTaskJournalEntry"/> instances. Returns an empty collection when no entries are found.</returns>
    IReadOnlyCollection<JobTaskJournalEntry> GetEntries(IJobTask jobTask);
}
