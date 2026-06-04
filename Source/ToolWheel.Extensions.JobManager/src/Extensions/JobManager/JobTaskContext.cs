using System;
using System.Reflection;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents the execution context for a single job task.
/// The context aggregates the job descriptor, the runtime job task handle, the invocation target and method,
/// and the evaluated condition status used by middleware and executors.
/// </summary>
public class JobTaskContext : IJobTaskContext
{
    private JobTask jobTask;

    /// <summary>
    /// Initializes a new instance of <see cref="JobTaskContext"/> that wraps the specified <paramref name="jobTask"/>.
    /// </summary>
    /// <param name="jobTask">The runtime job task that this context describes.</param>
    public JobTaskContext(JobTask jobTask)
    {
        this.jobTask = jobTask;
    }

    /// <summary>
    /// Gets the job descriptor associated with this execution context.
    /// </summary>
    public IJob Job { get => jobTask.Job; }

    /// <summary>
    /// Gets the runtime job task handle associated with this context.
    /// </summary>
    public IJobTask JobTask { get => jobTask; }

    /// <summary>
    /// Gets or sets the target instance on which the <see cref="TargetMethod"/> will be invoked, or <c>null</c> for static methods.
    /// Middleware may set this value prior to invocation.
    /// </summary>
    public object? TargetObject { get; internal set; }

    /// <summary>
    /// Gets the reflected method information that will be invoked for this job.
    /// </summary>
    public MethodInfo TargetMethod { get => jobTask.Job.TargetMethod; }

    /// <summary>
    /// Gets or sets the evaluated condition status for the job.
    /// Controllers and middleware set this value to indicate whether execution should proceed.
    /// </summary>
    public JobConditionStatus ConditionStatus { get; internal set; } = JobConditionStatus.Ready;

    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> used to resolve services during job execution.
    /// Set to the journal-aware provider by the execution service before the pipeline runs.
    /// </summary>
    public IServiceProvider ServiceProvider { get; internal set; } = null!;
}
