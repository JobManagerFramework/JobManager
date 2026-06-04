using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Represents a collection of <see cref="IJobDescription"/> instances.
/// Inherits from <see cref="ICollection{T}"/> and provides a typed container for job descriptions.
/// </summary>
public interface IJobDescriptionCollection : ICollection<IJobDescription>
{ }

