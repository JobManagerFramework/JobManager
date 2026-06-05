using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ToolWheel.Extensions.JobManager;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// In-memory implementation of <see cref="IJobOptionStorage"/>.
/// Replace with <c>EfExtensionOptionStorage</c> to persist extension options across restarts.
/// </summary>
public class InMemoryJobOptionStorage : IJobOptionStorage
{
    private readonly ConcurrentDictionary<IJob, ConcurrentDictionary<Type, IJobOption>> store = new();

    /// <inheritdoc/>
    public void Set(IJobOption option)
    {
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        var ownerStore = store.GetOrAdd(option.Job, _ => new ConcurrentDictionary<Type, IJobOption>());
        ownerStore[option.OptionType] = option;
    }

    /// <inheritdoc/>
    public bool TryAdd(IJobOption option)
    {
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        var ownerStore = store.GetOrAdd(option.Job, _ => new ConcurrentDictionary<Type, IJobOption>());
        return ownerStore.TryAdd(option.OptionType, option);
    }

    /// <inheritdoc/>
    public bool Remove(IJobOption option)
    {
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        if (!store.TryGetValue(option.Job, out var ownerStore))
        {
            return false;
        }

        return ownerStore.TryRemove(option.OptionType, out _);
    }

    /// <inheritdoc/>
    public void Clear(IJobOption option)
    {
        ArgumentNullException.ThrowIfNull(option, nameof(option));
        store.TryRemove(option.Job, out _);
    }

    /// <inheritdoc/>
    public IEnumerable<IJobOption> Get(IJob job)
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));

        if (!store.TryGetValue(job, out var ownerStore))
        {
            return Enumerable.Empty<IJobOption>();
        }

        return ownerStore.Values.ToList();
    }

    /// <inheritdoc/>
    public IEnumerable<IJobOption> Get(IJob job, Type optionType)
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));
        ArgumentNullException.ThrowIfNull(optionType, nameof(optionType));

        if (!store.TryGetValue(job, out var ownerStore))
        {
            return Enumerable.Empty<IJobOption>();
        }

        if (ownerStore.TryGetValue(optionType, out var jobOption))
        {
            return new[] { jobOption };
        }

        return Enumerable.Empty<IJobOption>();
    }

    /// <inheritdoc/>
    public IReadOnlyList<IJobOption> GetAll()
    {
        return store.Values
            .SelectMany(dict => dict.Values)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public IEnumerable<IJob> GetOwnerIds()
    {
        return store.Keys.ToArray();
    }
}
