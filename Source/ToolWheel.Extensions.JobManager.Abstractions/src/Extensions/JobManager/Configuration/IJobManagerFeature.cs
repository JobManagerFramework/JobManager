using System;
using Microsoft.Extensions.DependencyInjection;
using ToolWheel.Extensions.JobManager.Services;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Defines a feature that can be applied to a job description and job during registration. This interface allows for modular and extensible job configurations, enabling developers to create reusable features that can modify job behavior or add additional functionality when applied.
    /// </summary>
    public interface IJobManagerFeature
    {
        /// <summary>
        /// Applies the feature to the given job description and job. This method is called during job registration and can be used to modify the job description or perform additional setup based on the feature's functionality.
        /// </summary>
        /// <param name="serviceProvider">The application service provider.</param>
        /// <param name="jobDescription">The job description that owns this feature. Can be used to access sibling features.</param>
        /// <param name="job">The job instance that was just registered.</param>
        void Apply(IServiceProvider serviceProvider, IJobDescription jobDescription, IJob job)
        { }
    }
}
