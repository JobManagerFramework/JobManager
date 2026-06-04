using System;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Fluent builder for configuring a <see cref="JobDescription"/>.
    /// Provides convenience methods to set identity, metadata and features.
    /// </summary>
    public class JobDescriptionBuilder : IJobDescriptionBuilder
    {
        private JobDescription jobDescription;

        /// <summary>
        /// Initializes a new instance of <see cref="JobDescriptionBuilder"/> for the specified <paramref name="jobDescription"/>.
        /// </summary>
        /// <param name="jobDescription">The job description to configure.</param>
        public JobDescriptionBuilder(JobDescription jobDescription)
        {
            this.jobDescription = jobDescription;
        }

        /// <summary>
        /// Sets the textual description of the job.
        /// </summary>
        /// <param name="description">The description text to assign.</param>
        /// <returns>The same <see cref="IJobDescriptionBuilder"/> instance for fluent chaining.</returns>
        public IJobDescriptionBuilder Description(string description)
        {
            jobDescription.Description = description;

            return this;
        }

        /// <summary>
        /// Marks the job as disabled.
        /// </summary>
        /// <returns>The same <see cref="IJobDescriptionBuilder"/> instance for fluent chaining.</returns>
        public IJobDescriptionBuilder Disabled()
        {
            jobDescription.Enabled = false;
            return this;
        }

        /// <summary>
        /// Marks the job as enabled.
        /// </summary>
        /// <returns>The same <see cref="IJobDescriptionBuilder"/> instance for fluent chaining.</returns>
        public IJobDescriptionBuilder Enabled()
        {
            jobDescription.Enabled = true;
            return this;
        }

        /// <summary>
        /// Configures the job to use a singleton instance for the target object.
        /// When enabled, the same instance is reused across multiple job executions instead of creating a new one each time.
        /// </summary>
        /// <returns>The same <see cref="IJobDescriptionBuilder"/> instance for fluent chaining.</returns>
        public IJobDescriptionBuilder AsSingleton()
        {
            jobDescription.UseSingletonInstance = true;
            return this;
        }

        /// <summary>
        /// Sets the unique identifier of the job description.
        /// </summary>
        /// <param name="id">The identifier to assign. Must not be null or whitespace.</param>
        /// <returns>The same <see cref="IJobDescriptionBuilder"/> instance for fluent chaining.</returns>
        public IJobDescriptionBuilder Id(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));

            jobDescription.Id = id;

            return this;
        }

        /// <summary>
        /// Sets the display name of the job.
        /// </summary>
        /// <param name="name">The display name to assign. Must not be null or whitespace.</param>
        /// <returns>The same <see cref="IJobDescriptionBuilder"/> instance for fluent chaining.</returns>
        public IJobDescriptionBuilder Name(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

            jobDescription.Name = name;

            return this;
        }

        /// <summary>
        /// Configures a job-specific logger that will be used by the journal to forward
        /// lifecycle entries during job execution.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance to use for this job. Must not be <c>null</c>.</param>
        /// <returns>The same <see cref="IJobDescriptionBuilder"/> instance for fluent chaining.</returns>
        public IJobDescriptionBuilder WithLogger(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            jobDescription.JobLogger = logger;

            return this;
        }

        /// <summary>
        /// Configures or adds a feature instance of type <typeparamref name="TFeature"/> to the underlying <see cref="JobDescription"/>.
        /// The <paramref name="feature"/> action is invoked with the feature instance for configuration.
        /// </summary>
        /// <typeparam name="TFeature">Feature type that implements <see cref="IJobManagerFeature"/> and has a parameterless constructor.</typeparam>
        /// <param name="feature">Action that configures the feature instance.</param>
        /// <returns>The same <see cref="IJobDescriptionBuilder"/> instance for fluent chaining.</returns>
        public IJobDescriptionBuilder WithFeature<TFeature>(Action<TFeature> feature)
            where TFeature : IJobManagerFeature, new()
        {
            var featureInstance = jobDescription.GetFeature<TFeature>() ?? new TFeature();

            feature(featureInstance);

            jobDescription.SetFeature(featureInstance);

            return this;
        }
    }
}
