using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents a job that wraps a target <see cref="MethodInfo"/> and optional target object.
/// A job is identified by <see cref="Id"/> and exposes runtime metadata such as <see cref="Name"/>, 
/// <see cref="Description"/> and whether it is <see cref="Enabled"/>.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed record Job : IJob
{
    /// <summary>
    /// Initializes a new instance of <see cref="Job"/>.
    /// </summary>
    /// <param name="id">The unique identifier of the job.</param>
    /// <param name="targetObject">The instance that contains the target method; <c>null</c> for static methods.</param>
    /// <param name="targetMethod">The reflected method information to be executed by the job.</param>
    /// <param name="useSingletonInstance">
    /// When <c>true</c>, the same instance is reused across multiple job executions.
    /// </param>
    public Job(string id, object? targetObject, MethodInfo targetMethod, bool useSingletonInstance)
    {
        Id = id;
        TargetObject = targetObject;
        TargetMethod = targetMethod;
        UseSingletonInstance = useSingletonInstance;
        Name = targetMethod.Name;
    }

    /// <summary>
    /// Gets the unique identifier of the job.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the instance that contains the target method; <c>null</c> for static methods.
    /// </summary>
    public object? TargetObject { get; }

    /// <summary>
    /// Gets the reflected method information to be executed by the job.
    /// </summary>
    public MethodInfo TargetMethod { get; }

    /// <summary>
    /// Gets the name of the job. Defaults to the target method name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets or sets a short description for the job.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether the job is enabled and should be considered for execution.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the target instance should be treated as a singleton.
    /// When true, the same instance is reused across multiple job executions instead of creating a new one each time.
    /// </summary>
    public bool UseSingletonInstance { get; }

    /// <summary>
    /// Gets or sets an optional logger associated with this job instance.
    /// </summary>
    public ILogger? JobLogger { get; init; }

    /// <summary>
    /// Returns a hash code for this job based on its <see cref="Id"/>.
    /// </summary>
    /// <returns>An <see cref="int"/> hash code derived from <see cref="Id"/>.</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Returns a human readable representation of the job, including name and id.
    /// </summary>
    /// <returns>A string in the form "JobName={ModuleName}, JobId={Id}".</returns>
    public override string ToString()
    {
        return $"JobName={Name}, JobId={Id}";
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
