using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// In-memory implementation of <see cref="IExtensionOptionStorage"/>.
/// Replace with <c>EfExtensionOptionStorage</c> to persist extension options across restarts.
/// </summary>
public class InMemoryExtensionOptionStorage : IExtensionOptionStorage
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, (Type Declared, Type Concrete, object Item)>> store = new();

    /// <inheritdoc/>
    public void Set<T>(string ownerId, string itemKey, T item) where T : notnull
    {
        var ownerStore = store.GetOrAdd(ownerId, _ => new ConcurrentDictionary<string, (Type, Type, object)>());
        ownerStore[itemKey] = (typeof(T), item.GetType(), item);
    }

    /// <inheritdoc/>
    public bool TryAdd<T>(string ownerId, string itemKey, T item) where T : notnull
    {
        var ownerStore = store.GetOrAdd(ownerId, _ => new ConcurrentDictionary<string, (Type, Type, object)>());
        return ownerStore.TryAdd(itemKey, (typeof(T), item.GetType(), item));
    }

    /// <inheritdoc/>
    public bool Remove(string ownerId, string itemKey)
    {
        if (!store.TryGetValue(ownerId, out var ownerStore))
        {
            return false;
        }

        return ownerStore.TryRemove(itemKey, out _);
    }

    /// <inheritdoc/>
    public void Clear(string ownerId)
    {
        store.TryRemove(ownerId, out _);
    }

    /// <inheritdoc/>
    public T? Get<T>(string ownerId, string itemKey) where T : class
    {
        if (!store.TryGetValue(ownerId, out var ownerStore))
        {
            return null;
        }

        return ownerStore.TryGetValue(itemKey, out var entry) ? entry.Item as T : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll<T>(string ownerId) where T : class
    {
        if (!store.TryGetValue(ownerId, out var ownerStore))
        {
            return Array.Empty<T>();
        }

        var declaredType = typeof(T);

        return ownerStore.Values
            .Where(e => e.Declared == declaredType)
            .Select(e => (T)e.Item)
            .ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetOwnerIds()
    {
        return store.Keys.ToArray();
    }
}
