using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager;

public interface IJobOption
{
    IJob Job { get; }

    object Option { get; }

    Type OptionType { get; }
}
