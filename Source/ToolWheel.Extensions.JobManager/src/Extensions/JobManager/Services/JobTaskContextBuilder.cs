using System;
using System.Reflection;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Builds and exposes a mutable execution context for a job task.
/// Wraps a concrete <see cref="JobTaskContext"/> and provides accessors used by middleware and executors.
/// </summary>
public class JobTaskContextBuilder : IJobTaskContextBuilder
{
    private JobTaskContext jobTaskContext;

    /// <summary>
    /// Initializes a new instance of <see cref="JobTaskContextBuilder"/> that wraps the specified <paramref name="jobTaskContext"/>.
    /// </summary>
    /// <param name="jobTaskContext">The underlying <see cref="JobTaskContext"/> to wrap and modify.</param>
    public JobTaskContextBuilder(JobTaskContext jobTaskContext)
    {
        this.jobTaskContext = jobTaskContext;
    }

    /// <summary>
    /// Gets the job descriptor associated with the current execution context.
    /// </summary>
    public IJob Job { get => jobTaskContext.Job; }

    /// <summary>
    /// Gets the runtime job task handle associated with the current execution context.
    /// </summary>
    public IJobTask JobTask { get => jobTaskContext.JobTask; }

    /// <summary>
    /// Gets the reflected <see cref="MethodInfo"/> that will be invoked for this job task.
    /// </summary>
    public MethodInfo TargetMethod { get => jobTaskContext.TargetMethod; }

    /// <summary>
    /// Gets or sets the target instance on which <see cref="TargetMethod"/> will be invoked, or <c>null</c> for static methods.
    /// Middleware may create or replace this value prior to invocation.
    /// </summary>
    public object? TargetObject { get => jobTaskContext.TargetObject; set => jobTaskContext.TargetObject = value; }

    /// <summary>
    /// Gets or sets the evaluated condition status for the job execution.
    /// Controllers and middleware set this value to indicate whether execution should proceed.
    /// </summary>
    public JobConditionStatus ConditionStatus { get => jobTaskContext.ConditionStatus; set => jobTaskContext.ConditionStatus = value; }

    /// <inheritdoc/>
    public IServiceProvider ServiceProvider
    {
        get => jobTaskContext.ServiceProvider;
        internal set => jobTaskContext.ServiceProvider = value;
    }
}
