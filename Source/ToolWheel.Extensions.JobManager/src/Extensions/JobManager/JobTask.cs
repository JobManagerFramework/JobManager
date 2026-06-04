using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents a single execution instance of an <see cref="IJob"/>.
/// The record wraps the job, a unique task identifier and runtime state such as <see cref="Status"/>,
/// cancellation token, optional <see cref="Task"/> and result object.
/// </summary>
/// <param name="Job">The job that this task will execute or represents.</param>
/// <param name="Id">The unique identifier of the job task.</param>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed record JobTask(IJob Job, string Id) : IJobTask
{
    /// <summary>
    /// Gets the current status of the job task.
    /// Use the <see cref="JobTaskStatusEnum"/> to inspect state (e.g. Pending, Running, Completed, Faulted, Cancelled).
    /// </summary>
    public JobTaskStatusEnum Status { get; internal set; } = JobTaskStatusEnum.Pending;

    /// <summary>
    /// Gets the <see cref="CancellationTokenSource"/> that can be used to request cancellation of the running task.
    /// A new token source is created by default.
    /// </summary>
    public CancellationTokenSource CancellationToken { get; internal set; } = new CancellationTokenSource();

    /// <summary>
    /// Gets or sets the underlying <see cref="Task"/> instance that represents the asynchronous execution of the job.
    /// This value may be null if the task has not been started yet or if execution is synchronous.
    /// </summary>
    public Task? Task { get; internal set; }

    /// <summary>
    /// Gets or sets the result produced by the job execution.
    /// The concrete type depends on the executed method and may be null.
    /// </summary>
    public object? Result { get; internal set; }

    /// <inheritdoc/>
    public DateTime SignalTimestamp { get; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public DateTime? StartTimestamp { get; internal set; }

    /// <inheritdoc/>
    public DateTime? EndTimestamp { get; internal set; }

    /// <summary>
    /// Returns a hash code for this job task based on its <see cref="Id"/>.
    /// </summary>
    /// <returns>An <see cref="int"/> hash code derived from <see cref="Id"/>.</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Returns a human-readable representation of the job task, including its id, status and associated job.
    /// </summary>
    /// <returns>A string in the form "JobTaskId={Id}, Status={Status}, Job={Job}".</returns>
    public override string ToString()
    {
        return $"JobTaskId={Id}, Status={Status}, Job={Job}";
    }

    /// <summary>
    /// Provides the string used by the debugger display attribute.
    /// </summary>
    /// <returns>The same string as <see cref="ToString"/>.</returns>
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}
