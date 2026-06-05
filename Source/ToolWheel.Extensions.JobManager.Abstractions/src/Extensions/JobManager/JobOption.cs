using System;

namespace ToolWheel.Extensions.JobManager;

public sealed record JobOption : IJobOption
{
    public JobOption(IJob job, object option)
    {
        Job = job;
        Option = option;
        OptionType = option.GetType();
    }

    public IJob Job { get; private set; }

    public object Option { get; private set; }

    public Type OptionType { get; private set; }
}
