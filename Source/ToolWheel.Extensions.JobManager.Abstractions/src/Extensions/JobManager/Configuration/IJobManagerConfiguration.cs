using System;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Configuration entry point for the job manager.
    /// Exposes the collection of discovered or configured job descriptions.
    /// </summary>
    public interface IJobManagerConfiguration
    {
        /// <summary>
        /// Gets the collection of job descriptions that the job manager will consider.
        /// </summary>
        IJobDescriptionCollection Jobs { get; }

        /// <summary>
        /// Retrieves the specified feature associated with the job manager.
        /// </summary>
        /// <typeparam name="TFeature">The type of feature to retrieve, which must implement IJobManagerFeature.</typeparam>
        /// <returns>The feature instance if found; otherwise, null.</returns>
        TFeature? GetFeature<TFeature>()
            where TFeature : IJobManagerFeature;

        /// <summary>
        /// Associates a feature with the job manager.
        /// </summary>
        /// <typeparam name="TFeature">The type of feature to set, implementing IJobManagerFeature.</typeparam>
        /// <param name="feature">The feature instance to associate.</param>
        void SetFeature<TFeature>(TFeature feature)
            where TFeature : IJobManagerFeature;
    }
}
