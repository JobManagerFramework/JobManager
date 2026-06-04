using System;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Fluent builder interface for constructing or modifying an <see cref="IJobDescription"/>.
    /// </summary>
    public interface IJobDescriptionBuilder
    {
        /// <summary>
        /// Sets the unique identifier of the job description.
        /// </summary>
        /// <param name="id">The identifier to assign.</param>
        IJobDescriptionBuilder Id(string id);

        /// <summary>
        /// Sets the display name of the job.
        /// </summary>
        /// <param name="name">The display name to assign.</param>
        IJobDescriptionBuilder Name(string name);

        /// <summary>
        /// Sets a short textual description of the job's purpose or behavior.
        /// </summary>
        /// <param name="description">The description text.</param>
        IJobDescriptionBuilder Description(string description);

        /// <summary>
        /// Marks the job description as enabled.
        /// </summary>
        IJobDescriptionBuilder Enabled();

        /// <summary>
        /// Marks the job description as disabled.
        /// </summary>
        IJobDescriptionBuilder Disabled();

        /// <summary>
        /// Configures the job to use a singleton instance for the target object.
        /// When enabled, the same instance is reused across multiple job executions instead of creating a new one each time.
        /// </summary>
        IJobDescriptionBuilder AsSingleton();

        /// <summary>
        /// Configures a job-specific logger that will be used by the journal to forward
        /// lifecycle entries during job execution.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance to use for this job.</param>
        IJobDescriptionBuilder WithLogger(ILogger logger);

        /// <summary>
        /// Attaches or replaces a feature of type <typeparamref name="TFeature"/> on the job description.
        /// </summary>
        /// <typeparam name="TFeature">The job manager feature type.</typeparam>
        /// <param name="feature">The feature instance to attach.</param>
        IJobDescriptionBuilder WithFeature<TFeature>(Action<TFeature> feature)
            where TFeature : IJobManagerFeature, new();
    }
}
