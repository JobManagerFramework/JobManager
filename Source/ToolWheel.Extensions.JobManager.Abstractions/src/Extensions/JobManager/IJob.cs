using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Describes a job definition that references a target method and an optional target instance.
/// Implementations provide identity, metadata and runtime hints used by the job runtime.
/// </summary>
public interface IJob
{
    /// <summary>
    /// A short textual description of the job's purpose or behaviour.
    /// </summary>
    string Description { get; init; }

    /// <summary>
    /// Indicates whether the job is enabled and should be considered for scheduling and execution.
    /// </summary>
    bool Enabled { get; init; }

    /// <summary>
    /// The unique identifier of the job.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// An optional logger instance that is used to forward lifecycle and execution entries from the job.
    /// </summary>
    ILogger? JobLogger { get; init; }

    /// <summary>
    /// The display name of the job.
    /// </summary>
    string Name { get; init; }

    /// <summary>
    /// The reflected <see cref="MethodInfo"/> that will be invoked when the job is executed.
    /// </summary>
    MethodInfo TargetMethod { get; }

    /// <summary>
    /// The instance that contains the <see cref="TargetMethod"/>, or <c>null</c> for static methods.
    /// </summary>
    object? TargetObject { get; }

    /// <summary>
    /// When <c>true</c>, the runtime should treat the target instance as a singleton and reuse it across executions.
    /// </summary>
    bool UseSingletonInstance { get; }
}
