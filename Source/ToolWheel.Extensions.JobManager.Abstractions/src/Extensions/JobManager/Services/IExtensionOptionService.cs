using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service for managing extension options with CRUD operations.
/// Provides a higher-level abstraction over <see cref="Storage.IExtensionOptionStorage"/>.
/// </summary>
public interface IExtensionOptionService
{
    /// <summary>
    /// Creates or updates an extension option for the given owner.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    /// <param name="ownerId">The owner identifier (e.g. job ID or trigger ID).</param>
    /// <param name="itemKey">The item key, unique within the owner.</param>
    /// <param name="option">The option to store.</param>
    void Create<T>(string ownerId, string itemKey, T option) where T : notnull;

    /// <summary>
    /// Reads an extension option for the given owner and key.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="ownerId">The owner identifier.</param>
    /// <param name="itemKey">The item key.</param>
    /// <returns>The option if found; <c>null</c> otherwise.</returns>
    T? Read<T>(string ownerId, string itemKey) where T : class;

    /// <summary>
    /// Updates an extension option only if it already exists.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    /// <param name="ownerId">The owner identifier.</param>
    /// <param name="itemKey">The item key.</param>
    /// <param name="option">The updated option.</param>
    /// <returns><c>true</c> if the option was updated; <c>false</c> if it did not exist.</returns>
    bool Update<T>(string ownerId, string itemKey, T option) where T : notnull;

    /// <summary>
    /// Deletes an extension option for the given owner and key.
    /// </summary>
    /// <param name="ownerId">The owner identifier.</param>
    /// <param name="itemKey">The item key.</param>
    /// <returns><c>true</c> if the option was deleted; <c>false</c> if it did not exist.</returns>
    bool Delete(string ownerId, string itemKey);

    /// <summary>
    /// Reads all extension options of a specific type for the given owner.
    /// </summary>
    /// <typeparam name="T">The declared type to filter by.</typeparam>
    /// <param name="ownerId">The owner identifier.</param>
    /// <returns>A read-only list of options.</returns>
    IReadOnlyList<T> ReadAll<T>(string ownerId) where T : class;

    /// <summary>
    /// Deletes all extension options for the given owner.
    /// </summary>
    /// <param name="ownerId">The owner identifier.</param>
    void DeleteAll(string ownerId);

    /// <summary>
    /// Gets all owner identifiers that have at least one stored option.
    /// </summary>
    /// <returns>A read-only list of owner identifiers.</returns>
    IReadOnlyList<string> GetOwnerIds();
}
