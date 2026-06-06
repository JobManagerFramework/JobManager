using System;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents a sealed record implementation of <see cref="IJobOption"/> that encapsulates
/// a job and its associated option.
/// </summary>
public sealed record JobOption : IJobOption
{
    /// <summary>
    /// Initializes a new instance of <see cref="JobOption"/>.
    /// </summary>
    /// <param name="job">The job to which this option is associated.</param>
    /// <param name="option">The option instance.</param>
    public JobOption(IJob job, object option)
    {
        Job = job;
        Option = option;
        OptionType = option.GetType();
    }

    /// <summary>
    /// Gets the job associated with this option.
    /// </summary>
    public IJob Job { get; private set; }

    /// <summary>
    /// Gets the option instance.
    /// </summary>
    public object Option { get; private set; }

    /// <summary>
    /// Gets the type of the option.
    /// </summary>
    public Type OptionType { get; private set; }
}
