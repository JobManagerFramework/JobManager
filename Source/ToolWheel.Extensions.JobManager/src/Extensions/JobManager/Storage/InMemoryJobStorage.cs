using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// In-memory implementation of <see cref="IJobStorage"/>.
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread-safe access.
/// Replace this implementation with a database-backed variant to persist jobs across restarts.
/// </summary>
public class InMemoryJobStorage : IJobStorage
{
    private readonly ConcurrentDictionary<string, IJob> jobs = new ConcurrentDictionary<string, IJob>();

    /// <inheritdoc />
    public bool TryAdd(IJob job)
    {
        return jobs.TryAdd(job.Id, job);
    }

    /// <inheritdoc />
    public IJob? FindById(string jobId)
    {
        jobs.TryGetValue(jobId, out var job);
        return job;
    }

    /// <inheritdoc />
    public bool TryUpdate(string jobId, IJob updatedJob, IJob originalJob)
    {
        return jobs.TryUpdate(jobId, updatedJob, originalJob);
    }

    /// <inheritdoc />
    public bool TryRemove(string jobId)
    {
        return jobs.TryRemove(jobId, out _);
    }

    /// <inheritdoc />
    public IEnumerable<IJob> GetAll()
    {
        return jobs.Values;
    }
}
