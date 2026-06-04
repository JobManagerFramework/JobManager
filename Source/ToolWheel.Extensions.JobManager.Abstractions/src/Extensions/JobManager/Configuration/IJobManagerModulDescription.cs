using System;
using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Describes a registered module within the Job Manager.
/// Implementations are resolved by the Studio dashboard to display the list of active modules
/// and, optionally, to contribute a configuration tab to the Job Options modal.
/// </summary>
public interface IJobManagerModulDescription
{
    /// <summary>
    /// The unique display name of the module.
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// Configures the specified job manager builder with default or recommended settings.
    /// </summary>
    /// <param name="builder">The job manager configuration builder to be configured. Cannot be null.</param>
    void ModuleConfiguration(IJobManagerConfigurationBuilder builder);
}
