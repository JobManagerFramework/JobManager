using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// In-memory implementation of <see cref="IJobTaskJournalStorage"/>.
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by job task identifier for thread-safe access.
/// Replace this implementation with a database-backed variant to persist journal entries across restarts.
/// </summary>
public class InMemoryJobTaskJournalStorage : IJobTaskJournalStorage
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<JobTaskJournalEntry>> store
        = new ConcurrentDictionary<string, ConcurrentQueue<JobTaskJournalEntry>>();

    /// <inheritdoc />
    public void Append(string jobTaskId, JobTaskJournalEntry entry)
    {
        var queue = store.GetOrAdd(jobTaskId, _ => new ConcurrentQueue<JobTaskJournalEntry>());
        queue.Enqueue(entry);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<JobTaskJournalEntry> GetEntries(string jobTaskId)
    {
        if (store.TryGetValue(jobTaskId, out var queue))
        {
            return queue.ToArray();
        }

        return [];
    }
}
