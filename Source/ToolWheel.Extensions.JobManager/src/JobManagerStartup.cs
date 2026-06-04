using System;
using ToolWheel.Extensions.JobManager.Configuration;

namespace ToolWheel;

/// <summary>
/// Base class for configuring the JobManager and registering jobs.
/// </summary>
public abstract class JobManagerStartup
{
    /// <summary>
    /// Configures the JobManager with custom settings, services, and middleware.
    /// </summary>
    /// <param name="configure">The configuration builder for the JobManager.</param>
    public virtual void ConfigureJobManager(IJobManagerConfigurationBuilder configure)
    { }

    /// <summary>
    /// Registers and configures jobs for the JobManager.
    /// </summary>
    /// <param name="collection">The collection to which job descriptions are added.</param>
    public virtual void ConfigureJobs(IJobDescriptionCollection collection)
    { }
}
