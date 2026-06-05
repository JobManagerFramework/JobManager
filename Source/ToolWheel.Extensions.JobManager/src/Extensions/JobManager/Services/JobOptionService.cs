using System;
using System.Collections.Generic;
using System.Linq;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Default implementation of <see cref="IJobOptionService"/> backed by <see cref="IJobOptionStorage"/>.
/// </summary>
public class JobOptionService : IJobOptionService
{
    private readonly IJobOptionStorage storage;

    /// <summary>
    /// Initializes a new instance of <see cref="JobOptionService"/>.
    /// </summary>
    /// <param name="storage">The storage backend. Must not be <c>null</c>.</param>
    public JobOptionService(IJobOptionStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage, nameof(storage));
        this.storage = storage;
    }

    /// <inheritdoc />
    public void Add(IJob job, object option)
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        var jobOption = new JobOption(job, option);
        storage.Set(jobOption);
    }

    /// <inheritdoc />
    public void Add<T>(IJob job, T option) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        var jobOption = new JobOption(job, option);
        storage.Set(jobOption);
    }

    /// <inheritdoc />
    public bool Update<T>(IJob job, T option) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        var existing = storage.Get(job, typeof(T)).FirstOrDefault();
        if (existing == null)
        {
            return false;
        }

        var jobOption = new JobOption(job, option);
        storage.Set(jobOption);
        return true;
    }

    /// <inheritdoc />
    public bool Remove<T>(IJob job) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));

        var existing = storage.Get(job, typeof(T)).FirstOrDefault();
        if (existing == null)
        {
            return false;
        }

        return storage.Remove(existing);
    }

    /// <inheritdoc />
    public T? Get<T>(IJob job) where T : class
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));

        var jobOption = storage.Get(job, typeof(T)).FirstOrDefault();
        return jobOption?.Option as T;
    }

    /// <inheritdoc />
    public bool Contains<T>(IJob job) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));

        return storage.Get(job, typeof(T)).Any();
    }

    /// <inheritdoc />
    public IReadOnlyList<IJobOption> GetAll(IJob job)
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));

        return storage.Get(job).ToList().AsReadOnly();
    }
}

