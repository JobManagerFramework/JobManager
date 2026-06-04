using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Describes a job discovered or configured for execution.
    /// The description exposes the target method and instance, identity, metadata and optional features.
    /// </summary>
    public interface IJobDescription
    {
        /// <summary>
        /// Gets the target instance that contains the <see cref="TargetMethod"/>, or <c>null</c> for static methods.
        /// </summary>
        object? TargetObject { get; }

        /// <summary>
        /// Gets the reflected <see cref="MethodInfo"/> that will be executed for this job.
        /// </summary>
        MethodInfo TargetMethod { get; }

        /// <summary>
        /// Gets the unique identifier of the job description.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the display name of the job.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a short textual description of the job's purpose or behavior.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets a value indicating whether the job is enabled and should be considered for execution.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Gets a value indicating whether the target instance should be treated as a singleton.
        /// When true, the same instance is reused across multiple job executions instead of creating a new one each time.
        /// </summary>
        bool UseSingletonInstance { get; }

        /// <summary>
        /// Gets an optional job-specific <see cref="ILogger"/> instance.
        /// When set, this logger is used by the journal to forward lifecycle entries during job execution.
        /// </summary>
        ILogger? JobLogger { get; }

        /// <summary>
        /// Gets the collection of features attached to this job description.
        /// </summary>
        IEnumerable<IJobManagerFeature> Features { get; }

        /// <summary>
        /// Retrieves an attached feature of type <typeparamref name="TFeature"/> if present.
        /// </summary>
        /// <typeparam name="TFeature">The feature type to retrieve.</typeparam>
        /// <returns>The feature instance or <c>null</c> when not available.</returns>
        TFeature? GetFeature<TFeature>()
            where TFeature : IJobManagerFeature;
    }
}
