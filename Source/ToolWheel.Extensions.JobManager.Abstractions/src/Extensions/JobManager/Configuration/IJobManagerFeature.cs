using System;
using Microsoft.Extensions.DependencyInjection;
using ToolWheel.Extensions.JobManager.Services;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Marker interface for features that can be attached to a job description or job manager configuration.
    /// </summary>
    /// <remarks>
    /// Implementations represent optional, extensible functionality or metadata that can be associated with jobs.
    /// The default <see cref="Apply"/> implementation stores the feature in <see cref="IJobExecutionConditionService"/>
    /// using the concrete runtime type as the key. Override <see cref="Apply"/> to perform additional or different registration logic.
    /// </remarks>
    public interface IJobManagerFeature
    {
        /// <summary>
        /// Called when the owning job is added to the job service.
        /// The default implementation stores this feature in <see cref="IJobExecutionConditionService"/>
        /// using the feature's concrete runtime type as the key.
        /// Override to perform additional or different registration logic.
        /// </summary>
        /// <param name="serviceProvider">The application service provider.</param>
        /// <param name="jobDescription">The job description that owns this feature. Can be used to access sibling features.</param>
        /// <param name="job">The job instance that was just registered.</param>
        void Apply(IServiceProvider serviceProvider, IJobDescription jobDescription, IJob job)
        {
            serviceProvider.GetService<IJobExecutionConditionService>()?.Add(job, this);
        }
    }   
}
