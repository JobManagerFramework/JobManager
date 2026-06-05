using System.Collections.Generic;

namespace ToolWheel.Extensions.JobManager.Storage;

public interface IOptionStorage
{
    void Clear(IJobOption option);
    IJobOption? Get(IJobOption option);
    IReadOnlyList<IJobOption> GetAll(IJobOption option);
    IEnumerable<IJob> GetOwnerIds();
    bool Remove(IJobOption option);
    void Set(IJobOption option);
    bool TryAdd(IJobOption option);
}