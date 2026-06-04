using System;

namespace ToolWheel.Extensions.JobManager.Conditions;

/// <summary>
/// A builder used by <see cref="IExecutionCondition"/> implementations to construct a
/// <see cref="JobConditionStatus"/> during asynchronous evaluation.
/// Provides a fluent API to set the status and an optional message.
/// </summary>
public sealed class ExecutionConditionContext
{
    private JobConditionStatusEnum _status = JobConditionStatus.Ready.Status;
    private string _message = JobConditionStatus.Ready.Message;

    /// <summary>
    /// Initializes a new instance of <see cref="ExecutionConditionContext"/> for the specified job and signal timestamp.
    /// </summary>
    /// <param name="job">The job descriptor that is being evaluated.</param>
    /// <param name="signalTimestamp">The UTC timestamp of the evaluation signal that triggered this context.</param>
    public ExecutionConditionContext(IJob job, DateTime signalTimestamp)
    {
        Job = job;
        SignalTimestamp = signalTimestamp;
    }

    /// <summary>
    /// Marks the condition as ready for execution with an optional message.
    /// </summary>
    /// <param name="message">An optional message describing why the condition is ready.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ExecutionConditionContext SetReady(string? message = null)
    {
        _status = JobConditionStatusEnum.Ready;
        _message = message ?? JobConditionStatus.Ready.Message;
        return this;
    }

    /// <summary>
    /// Marks the condition as not ready for execution with an optional message.
    /// </summary>
    /// <param name="message">An optional message describing why the condition is not ready.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ExecutionConditionContext SetNotReady(string? message = null)
    {
        _status = JobConditionStatusEnum.NotReady;
        _message = message ?? JobConditionStatus.NotReady.Message;
        return this;
    }

    /// <summary>
    /// Builds the final <see cref="JobConditionStatus"/> from the current builder state.
    /// </summary>
    /// <returns>A new <see cref="JobConditionStatus"/> reflecting the configured status and message.</returns>
    public JobConditionStatus BuildConditionStatus()
    {
        return new JobConditionStatus(_status, _message);
    }

    /// <summary>
    /// Gets the current condition status value that has been set via <see cref="SetReady"/> or <see cref="SetNotReady"/>.
    /// </summary>
    public JobConditionStatusEnum Status => _status;

    /// <summary>
    /// Gets the job descriptor that is being evaluated.
    /// </summary>
    public IJob Job { get; }

    /// <summary>
    /// Gets the UTC timestamp of the evaluation signal that triggered this context.
    /// </summary>
    public DateTime SignalTimestamp { get; }
}
