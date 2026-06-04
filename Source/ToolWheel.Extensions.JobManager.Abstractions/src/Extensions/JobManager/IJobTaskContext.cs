using System.Reflection;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents the execution context for a single job task.
/// The context aggregates the job descriptor, its runtime task instance, the reflected target method and object,
/// and the evaluated condition status required by schedulers and executors.
/// </summary>
public interface IJobTaskContext
{
    /// <summary>
    /// Gets the job descriptor associated with this execution context.
    /// </summary>
    public IJob Job { get; }

    /// <summary>
    /// Gets the runtime task instance that represents the current execution of the job.
    /// </summary>
    public IJobTask JobTask { get; }

    /// <summary>
    /// Gets the target instance on which <see cref="TargetMethod"/> will be invoked, or <c>null</c> for static methods.
    /// </summary>
    public object? TargetObject { get; }

    /// <summary>
    /// Gets the reflected <see cref="MethodInfo"/> that will be executed for this job.
    /// </summary>
    public MethodInfo TargetMethod { get; }

    /// <summary>
    /// Gets the evaluation result of the job's preconditions or execution conditions.
    /// Consumers can inspect this value to decide whether execution should proceed.
    /// </summary>
    public JobConditionStatus ConditionStatus { get; }
}
