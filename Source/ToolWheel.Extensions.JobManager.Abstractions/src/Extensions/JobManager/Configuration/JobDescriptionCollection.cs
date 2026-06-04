using System.Collections;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// A simple in-memory collection of <see cref="IJobDescription"/> instances.
/// Implements <see cref="IJobDescriptionCollection"/> and forwards operations to an internal list.
/// </summary>
public class JobDescriptionCollection : IJobDescriptionCollection
{
    private readonly List<IJobDescription> _jobDescriptions = new List<IJobDescription>();

    /// <summary>
    /// Gets the number of job descriptions contained in the collection.
    /// </summary>
    public int Count { get => _jobDescriptions.Count; }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// This implementation always returns <c>false</c>.
    /// </summary>
    public bool IsReadOnly { get => false; }

    /// <summary>
    /// Adds the specified <paramref name="item"/> to the collection.
    /// </summary>
    /// <param name="item">The job description to add.</param>
    public void Add(IJobDescription item)
    {
        _jobDescriptions.Add(item);
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public void Clear()
    {
        _jobDescriptions.Clear();
    }

    /// <summary>
    /// Determines whether the collection contains the specified <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The job description to locate in the collection.</param>
    /// <returns><c>true</c> if the item is found; otherwise <c>false</c>.</returns>
    public bool Contains(IJobDescription item)
    {
        return _jobDescriptions.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the collection to the specified array starting at the given index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    public void CopyTo(IJobDescription[] array, int arrayIndex)
    {
        _jobDescriptions.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<IJobDescription> GetEnumerator()
    {
        return _jobDescriptions.GetEnumerator();
    }

    /// <summary>
    /// Removes the first occurrence of a specific <paramref name="item"/> from the collection.
    /// </summary>
    /// <param name="item">The job description to remove.</param>
    /// <returns><c>true</c> if the item was successfully removed; otherwise <c>false</c>.</returns>
    public bool Remove(IJobDescription item)
    {
        return _jobDescriptions.Remove(item);
    }

    /// <summary>
    /// Returns a non-generic enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> for the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
