using System.Collections.Generic;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Default implementation of <see cref="IJobExecutionConditionService"/> backed by <see cref="IExtensionOptionStorage"/>.
/// </summary>
public class JobExecutionConditionService : IJobExecutionConditionService
{
    private readonly IExtensionOptionStorage storage;

    /// <summary>
    /// Initializes a new instance of <see cref="JobExecutionConditionService"/>.
    /// </summary>
    /// <param name="storage">The storage backend. Must not be <c>null</c>.</param>
    public JobExecutionConditionService(IExtensionOptionStorage storage)
    {
        this.storage = storage;
    }

    /// <inheritdoc />
    public void Add(IJob job, IJobManagerFeature feature)
    {
        storage.Set<IJobManagerFeature>(job.Id, feature.GetType().FullName!, feature);
    }

    /// <inheritdoc />
    public void Add<T>(IJob job, T feature) where T : IJobManagerFeature
    {
        storage.Set<IJobManagerFeature>(job.Id, typeof(T).FullName!, feature);
    }

    /// <inheritdoc />
    public bool Update<T>(IJob job, T feature) where T : IJobManagerFeature
    {
        var key = typeof(T).FullName!;

        if (storage.Get<IJobManagerFeature>(job.Id, key) == null)
        {
            return false;
        }

        storage.Set<IJobManagerFeature>(job.Id, key, feature);
        return true;
    }

    /// <inheritdoc />
    public bool Remove<T>(IJob job) where T : IJobManagerFeature
    {
        return storage.Remove(job.Id, typeof(T).FullName!);
    }

    /// <inheritdoc />
    public T? Get<T>(IJob job) where T : class, IJobManagerFeature
    {
        return storage.Get<IJobManagerFeature>(job.Id, typeof(T).FullName!) as T;
    }

    /// <inheritdoc />
    public bool Contains<T>(IJob job) where T : IJobManagerFeature
    {
        return storage.Get<IJobManagerFeature>(job.Id, typeof(T).FullName!) is not null;
    }

    /// <inheritdoc />
    public IReadOnlyList<IJobManagerFeature> GetAll(IJob job)
    {
        return storage.GetAll<IJobManagerFeature>(job.Id);
    }
}
