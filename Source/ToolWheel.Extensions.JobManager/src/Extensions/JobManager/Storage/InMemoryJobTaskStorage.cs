using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// In-memory implementation of <see cref="IJobTaskStorage"/>.
/// Uses nested <see cref="ConcurrentDictionary{TKey,TValue}"/> instances for thread-safe access.
/// Replace this implementation with a database-backed variant to persist job tasks across restarts.
/// </summary>
public class InMemoryJobTaskStorage : IJobTaskStorage
{
    private readonly ConcurrentDictionary<IJob, ConcurrentDictionary<string, IJobTask>> jobTasks = new ConcurrentDictionary<IJob, ConcurrentDictionary<string, IJobTask>>();

    /// <inheritdoc />
    public void Add(IJob job, IJobTask jobTask)
    {
        var dict = GetOrCreate(job);
        dict.TryAdd(jobTask.Id, jobTask);
    }

    /// <inheritdoc />
    public IJobTask? FindByTaskId(IJob job, string jobTaskId)
    {
        if (jobTasks.TryGetValue(job, out var dict) && dict.TryGetValue(jobTaskId, out var jt))
        {
            return jt;
        }

        return null;
    }

    /// <inheritdoc />
    public bool TryRemove(IJobTask jobTask)
    {
        var dict = GetOrCreate(jobTask.Job!);
        return dict.TryRemove(jobTask.Id, out _);
    }

    /// <inheritdoc />
    public IEnumerable<IJobTask> GetAll()
    {
        var result = new List<IJobTask>();
        foreach (var pair in jobTasks)
        {
            result.AddRange(pair.Value.Values);
        }

        return result;
    }

    /// <inheritdoc />
    public IEnumerable<IJobTask> GetByJob(IJob job)
    {
        return GetOrCreate(job).Values.ToArray();
    }

    /// <inheritdoc />
    public IEnumerable<IJobTask> GetByJob(IJob job, IEnumerable<JobTaskStatusEnum> statuses)
    {
        var statusSet = new HashSet<JobTaskStatusEnum>(statuses);

        if (statusSet.Count == 0)
        {
            return GetOrCreate(job).Values.ToArray();
        }

        return GetOrCreate(job).Values.Where(t => statusSet.Contains(t.Status)).ToArray();
    }

    private ConcurrentDictionary<string, IJobTask> GetOrCreate(IJob job)
    {
        return jobTasks.GetOrAdd(job, _ => new ConcurrentDictionary<string, IJobTask>());
    }
}
