using System;
using ToolWheel.Extensions.JobManager.Configuration;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Adapter that wraps <see cref="IJobManagerFeature"/> as <see cref="IJobOption"/>.
/// </summary>
internal class JobFeatureOption : IJobOption
{
    private readonly IJobManagerFeature feature;

    /// <summary>
    /// Initializes a new instance of <see cref="JobFeatureOption"/>.
    /// </summary>
    /// <param name="job">The job associated with this option.</param>
    /// <param name="feature">The job manager feature to wrap.</param>
    public JobFeatureOption(IJob job, IJobManagerFeature feature)
    {
        this.Job = job;
        this.feature = feature;
    }

    /// <inheritdoc />
    public IJob Job { get; }

    /// <inheritdoc />
    public Type OptionType => feature.GetType();

    /// <inheritdoc />
    public object Option => feature;
}
