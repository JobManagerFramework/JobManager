namespace ToolWheel.Extensions.JobManager.Storage;

/// <summary>
/// Represents a single entry returned by <see cref="IExtensionOptionStorage.GetAllEntries"/>.
/// </summary>
/// <param name="ItemKey">The item key, unique within the owner.</param>
/// <param name="TypeName">The fully qualified concrete runtime type name of the stored item.</param>
/// <param name="Item">The deserialized item instance.</param>
public sealed record StorageEntry(string ItemKey, string TypeName, object Item);
