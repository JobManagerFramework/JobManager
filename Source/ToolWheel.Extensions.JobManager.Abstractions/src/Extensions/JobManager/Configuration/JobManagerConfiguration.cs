using System;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Configuration container for the job manager.
    /// Exposes the collection of job descriptions that will be considered by the manager.
    /// </summary>
    public class JobManagerConfiguration : IJobManagerConfiguration
    {
        private readonly Dictionary<Type, IJobManagerFeature> features  = new Dictionary<Type, IJobManagerFeature>();

        /// <summary>
        /// Initializes a new instance of <see cref="JobManagerConfiguration"/> with an empty <see cref="JobDescriptionCollection"/>.
        /// </summary>
        public JobManagerConfiguration()
        {
            Jobs = new JobDescriptionCollection();
        }

        /// <summary>
        /// Gets the collection of configured or discovered job descriptions.
        /// </summary>
        public IJobDescriptionCollection Jobs { get; }


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
        /// Attaches or replaces a feature on this configuration.
        /// </summary>
        /// <typeparam name="TFeature">The feature type to set.</typeparam>
        /// <param name="feature">The feature instance to attach.</param>
        public void SetFeature<TFeature>(TFeature feature)
            where TFeature : IJobManagerFeature
        {
            features[typeof(TFeature)] = feature;
        }
    }
}
