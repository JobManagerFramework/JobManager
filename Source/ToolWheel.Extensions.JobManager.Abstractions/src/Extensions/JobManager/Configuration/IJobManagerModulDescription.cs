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
    string Name { get; }
}
