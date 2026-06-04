using System;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents a runtime handle for a single execution instance of an <see cref="IJob"/>.
/// Implementations expose the originating <see cref="Job"/>, a unique task <see cref="Id"/> and
/// runtime state required by schedulers and consumers (status, cancellation, task instance and result).
/// </summary>
public interface IJobTask
{
    /// <summary>
    /// Gets the associated job descriptor that this task executes or represents.
    /// </summary>
    IJob Job { get; }

    /// <summary>
    /// Gets the unique identifier of the job task.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the current execution status of the job task.
    /// See <see cref="JobTaskStatusEnum"/> for possible values (Pending, Running, Success, Failed, etc.).
    /// </summary>
    JobTaskStatusEnum Status { get; }

    /// <summary>
    /// Gets the <see cref="CancellationTokenSource"/> that can be used to request cancellation of the running task.
    /// Consumers may call <see cref="CancellationTokenSource.Cancel()"/> to request cancellation.
    /// </summary>
    CancellationTokenSource CancellationToken { get; }

    /// <summary>
    /// Gets the underlying <see cref="Task"/> instance that represents the asynchronous execution of the job.
    /// May be <c>null</c> if the execution has not been started or if the job runs synchronously.
    /// </summary>
    Task? Task { get; }

    /// <summary>
    /// Gets the result produced by the job execution. The concrete type depends on the executed method and may be <c>null</c>.
    /// </summary>
    object? Result { get; }

    /// <summary>
    /// Gets the timestamp indicating when the signal was generated.
    /// </summary>
    DateTime SignalTimestamp { get; }

    /// <summary>
    /// Gets the timestamp indicating when the operation started, if available.
    /// </summary>
    DateTime? StartTimestamp { get; }

    /// <summary>
    /// Gets the timestamp indicating when the operation ended, if available.
    /// </summary>
    DateTime? EndTimestamp { get; }
}
