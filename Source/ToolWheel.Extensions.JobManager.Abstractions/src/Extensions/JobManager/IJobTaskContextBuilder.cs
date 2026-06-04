using System;
using System.Reflection;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Builder interface that aggregates information required to construct an <see cref="IJobTaskContext"/>.
/// Implementations allow configuring the target method, optional target object, the associated job and job task,
/// and the evaluated condition status before creating the final context instance.
/// </summary>
public interface IJobTaskContextBuilder
{
    /// <summary>
    /// Gets the reflected <see cref="MethodInfo"/> that will be invoked when the context is executed.
    /// </summary>
    MethodInfo TargetMethod { get; }

    /// <summary>
    /// Gets or sets the target instance on which <see cref="TargetMethod"/> will be invoked, or <c>null</c> for static methods.
    /// </summary>
    object? TargetObject { get; set; }

    /// <summary>
    /// Gets the job descriptor associated with the context being built.
    /// </summary>
    IJob Job { get; }

    /// <summary>
    /// Gets the runtime job task that represents the execution instance associated with this context.
    /// </summary>
    IJobTask JobTask { get; }

    /// <summary>
    /// Gets or sets the evaluated condition status for the job. Consumers use this to determine whether execution should proceed.
    /// </summary>
    JobConditionStatus ConditionStatus { get; set; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> to use for resolving services during job execution.
    /// This is set to the journal-aware provider so that resolved instances (e.g. the job class) receive
    /// loggers that write to the job task journal.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}
