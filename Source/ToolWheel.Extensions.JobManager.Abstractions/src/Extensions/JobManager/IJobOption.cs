using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents an option or feature associated with a job.
/// </summary>
public interface IJobOption
{
    /// <summary>
    /// Gets the job associated with this option.
    /// </summary>
    IJob Job { get; }

    /// <summary>
    /// Gets the option instance.
    /// </summary>
    object Option { get; }

    /// <summary>
    /// Gets the type of the option.
    /// </summary>
    Type OptionType { get; }
}
