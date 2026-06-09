using System;
using System.Collections.Generic;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Default implementation of <see cref="IExtensionOptionService"/>.
/// Provides CRUD operations over <see cref="IExtensionOptionStorage"/>.
/// </summary>
public class ExtensionOptionService : IExtensionOptionService
{
    private readonly IExtensionOptionStorage storage;

    /// <summary>
    /// Initializes a new instance of <see cref="ExtensionOptionService"/>.
    /// </summary>
    /// <param name="storage">The storage backend. Must not be <c>null</c>.</param>
    public ExtensionOptionService(IExtensionOptionStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage, nameof(storage));
        this.storage = storage;
    }

    /// <inheritdoc/>
    public void Create<T>(string ownerId, string itemKey, T option) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(ownerId, nameof(ownerId));
        ArgumentNullException.ThrowIfNull(itemKey, nameof(itemKey));
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        storage.Set(ownerId, itemKey, option);
    }

    /// <inheritdoc/>
    public T? Read<T>(string ownerId, string itemKey) where T : class
    {
        ArgumentNullException.ThrowIfNull(ownerId, nameof(ownerId));
        ArgumentNullException.ThrowIfNull(itemKey, nameof(itemKey));

        return storage.Get<T>(ownerId, itemKey);
    }

    /// <inheritdoc/>
    public bool Update<T>(string ownerId, string itemKey, T option) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(ownerId, nameof(ownerId));
        ArgumentNullException.ThrowIfNull(itemKey, nameof(itemKey));
        ArgumentNullException.ThrowIfNull(option, nameof(option));

        if (!typeof(T).IsValueType && typeof(T).IsClass)
        {
            var existing = storage.Get<object>(ownerId, itemKey);
            if (existing is null)
            {
                return false;
            }

            storage.Set(ownerId, itemKey, option);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool Delete(string ownerId, string itemKey)
    {
        ArgumentNullException.ThrowIfNull(ownerId, nameof(ownerId));
        ArgumentNullException.ThrowIfNull(itemKey, nameof(itemKey));

        return storage.Remove(ownerId, itemKey);
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> ReadAll<T>(string ownerId) where T : class
    {
        ArgumentNullException.ThrowIfNull(ownerId, nameof(ownerId));

        return storage.GetAll<T>(ownerId);
    }

    /// <inheritdoc/>
    public void DeleteAll(string ownerId)
    {
        ArgumentNullException.ThrowIfNull(ownerId, nameof(ownerId));

        storage.Clear(ownerId);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetOwnerIds()
    {
        return storage.GetOwnerIds();
    }
}
