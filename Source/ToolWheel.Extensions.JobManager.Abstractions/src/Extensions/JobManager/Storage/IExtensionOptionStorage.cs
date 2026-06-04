using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// Generic storage abstraction for serializable extension options, keyed by owner ID and item key.
/// Callers are responsible for key uniqueness across different item categories within the same owner.
/// </summary>
public interface IExtensionOptionStorage
{
    /// <summary>
    /// Adds or replaces the item for the given owner and key.
    /// </summary>
    /// <typeparam name="T">The declared type used to group items for <see cref="GetAll{T}"/> queries.</typeparam>
    /// <param name="ownerId">The owner identifier (e.g. job ID or trigger ID).</param>
    /// <param name="itemKey">The item key, unique within the owner.</param>
    /// <param name="item">The item to store.</param>
    void Set<T>(string ownerId, string itemKey, T item) where T : notnull;

    /// <summary>
    /// Attempts to add the item without overwriting an existing entry.
    /// </summary>
    /// <typeparam name="T">The declared type used to group items for <see cref="GetAll{T}"/> queries.</typeparam>
    /// <param name="ownerId">The owner identifier.</param>
    /// <param name="itemKey">The item key, unique within the owner.</param>
    /// <param name="item">The item to store.</param>
    /// <returns><c>true</c> if the item was added; <c>false</c> if an entry with the same key already exists.</returns>
    bool TryAdd<T>(string ownerId, string itemKey, T item) where T : notnull;

    /// <summary>
    /// Removes the entry identified by <paramref name="ownerId"/> and <paramref name="itemKey"/>.
    /// </summary>
    /// <param name="ownerId">The owner identifier.</param>
    /// <param name="itemKey">The item key.</param>
    /// <returns><c>true</c> if the entry was removed; <c>false</c> if it did not exist.</returns>
    bool Remove(string ownerId, string itemKey);

    /// <summary>
    /// Removes all entries for the given <paramref name="ownerId"/>.
    /// </summary>
    /// <param name="ownerId">The owner identifier.</param>
    void Clear(string ownerId);

    /// <summary>
    /// Returns the item for the given owner and key, deserialized as <typeparamref name="T"/>.
    /// Returns <c>null</c> if not found.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="ownerId">The owner identifier.</param>
    /// <param name="itemKey">The item key.</param>
    T? Get<T>(string ownerId, string itemKey) where T : class;

    /// <summary>
    /// Returns all items stored under <paramref name="ownerId"/> that were added with declared type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The declared type to filter by.</typeparam>
    /// <param name="ownerId">The owner identifier.</param>
    IReadOnlyList<T> GetAll<T>(string ownerId) where T : class;

    /// <summary>
    /// Returns the identifiers of all owners that have at least one stored entry.
    /// </summary>
    IReadOnlyList<string> GetOwnerIds();

    /// <summary>
    /// Returns all entries stored under <paramref name="ownerId"/> regardless of declared type,
    /// each including its item key, concrete type name and item instance.
    /// </summary>
    /// <param name="ownerId">The owner identifier.</param>
    IReadOnlyList<StorageEntry> GetAllEntries(string ownerId);
}
