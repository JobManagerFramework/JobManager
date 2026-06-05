using System.Collections.Generic;
using System.Linq;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Default implementation of <see cref="IJobExecutionConditionService"/> backed by <see cref="IJobOptionStorage"/>.
/// </summary>
public class JobExecutionConditionService : IJobExecutionConditionService
{
    private readonly IJobOptionStorage storage;

    /// <summary>
    /// Initializes a new instance of <see cref="JobExecutionConditionService"/>.
    /// </summary>
    /// <param name="storage">The storage backend. Must not be <c>null</c>.</param>
    public JobExecutionConditionService(IJobOptionStorage storage)
    {
        this.storage = storage;
    }

    /// <inheritdoc />
    public void Add(IJob job, IJobManagerFeature feature)
    {
        var jobOption = new JobOption(job, feature);
        storage.Set(jobOption);
    }

    /// <inheritdoc />
    public void Add<T>(IJob job, T feature) where T : IJobManagerFeature
    {
        var jobOption = new JobOption(job, feature);
        storage.Set(jobOption);
    }

    /// <inheritdoc />
    public bool Update<T>(IJob job, T feature) where T : IJobManagerFeature
    {
        var existing = storage.Get(job, typeof(T)).FirstOrDefault();
        if (existing == null)
        {
            return false;
        }

        var jobOption = new JobOption(job, feature);
        storage.Set(jobOption);
        return true;
    }

    /// <inheritdoc />
    public bool Remove<T>(IJob job) where T : IJobManagerFeature
    {
        var existing = storage.Get(job, typeof(T)).FirstOrDefault();
        if (existing == null)
        {
            return false;
        }

        return storage.Remove(existing);
    }

    /// <inheritdoc />
    public T? Get<T>(IJob job) where T : class, IJobManagerFeature
    {
        var jobOption = storage.Get(job, typeof(T)).FirstOrDefault();
        return jobOption?.Option as T;
    }

    /// <inheritdoc />
    public bool Contains<T>(IJob job) where T : IJobManagerFeature
    {
        return storage.Get(job, typeof(T)).Any();
    }

    /// <inheritdoc />
    public IReadOnlyList<IJobManagerFeature> GetAll(IJob job)
    {
        return storage.Get(job)
            .Select(o => o.Option as IJobManagerFeature)
            .OfType<IJobManagerFeature>()
            .ToList();
    }
}


