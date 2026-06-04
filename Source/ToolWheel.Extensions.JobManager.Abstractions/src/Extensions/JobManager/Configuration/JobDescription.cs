using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Describes a job discovered or configured for execution.
    /// The record exposes the target instance and method, identity, metadata and attached features.
    /// </summary>
    /// <param name="TargetObject">The target instance that contains the <see cref="TargetMethod"/>, or <c>null</c> for static methods.</param>
    /// <param name="TargetMethod">The reflected <see cref="MethodInfo"/> that will be executed for this job.</param>
    public sealed record JobDescription(object? TargetObject, MethodInfo TargetMethod) : IJobDescription
    {
        private Dictionary<Type, IJobManagerFeature> features { get; } = new();

        /// <summary>
        /// Gets the unique identifier of the job description. Defaults to a new GUID.
        /// </summary>
        public string Id { get; internal set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets the display name of the job. Defaults to the target method name.
        /// </summary>
        public string Name { get; internal set; } = TargetMethod.Name;

        /// <summary>
        /// Gets a short textual description of the job's purpose or behavior.
        /// </summary>
        public string Description { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the job is enabled and should be considered for execution.
        /// </summary>
        public bool Enabled { get; internal set; } = true;

        /// <summary>
        /// Gets a value indicating whether the target instance should be treated as a singleton.
        /// When true, the same instance is reused across multiple job executions instead of creating a new one each time.
        /// </summary>
        public bool UseSingletonInstance { get; internal set; }

        /// <summary>
        /// Gets an optional job-specific <see cref="ILogger"/> instance.
        /// When set, this logger is used by the journal to forward lifecycle entries during job execution.
        /// </summary>
        public ILogger? JobLogger { get; internal set; }

        /// <summary>
        /// Gets the collection of features attached to this job description.
        /// </summary>
        public IEnumerable<IJobManagerFeature> Features
        {
            get
            {
                return features.Values;
            }
            internal set
            {
                foreach (var feature in value)
                {
                    features[feature.GetType()] = feature;
                }
            }
        }

        /// <summary>
        /// Retrieves a feature of the specified type if present.
        /// </summary>
        /// <typeparam name="TFeature">The feature type to retrieve.</typeparam>
        /// <returns>The feature instance or <c>null</c> when not available.</returns>
        public TFeature? GetFeature<TFeature>()
            where TFeature : IJobManagerFeature
        {
            features.TryGetValue(typeof(TFeature), out var feature);
            return (TFeature?)feature;
        }

        /// <summary>
        /// Attaches or replaces a feature on this job description.
        /// </summary>
        /// <typeparam name="TFeature">The feature type to set.</typeparam>
        /// <param name="feature">The feature instance to attach.</param>
        internal void SetFeature<TFeature>(TFeature feature)
            where TFeature : IJobManagerFeature
        {
            features[typeof(TFeature)] = feature;
        }
    }
}
