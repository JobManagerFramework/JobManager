using System;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents the evaluation result of a job condition.
/// The record contains a <see cref="JobConditionStatusEnum"/> value and an explanatory <see cref="Message"/>.
/// </summary>
/// <param name="Status">The condition status enum value.</param>
/// <param name="Message">A human-readable message describing the status.</param>
public sealed record JobConditionStatus(JobConditionStatusEnum Status, string Message) : IEquatable<JobConditionStatus>
{
    /// <summary>
    /// A singleton instance indicating that the job is ready for execution.
    /// </summary>
    public static readonly JobConditionStatus Ready = new(JobConditionStatusEnum.Ready, "Job is ready for execution.");

    /// <summary>
    /// A singleton instance indicating that the job is not ready for execution.
    /// </summary>
    public static readonly JobConditionStatus NotReady = new(JobConditionStatusEnum.NotReady, "Job is not ready for execution.");

    /// <summary>
    /// Determines whether this instance is equal to another <see cref="JobConditionStatus"/>.
    /// Equality is based on the <see cref="Status"/> value only.
    /// </summary>
    /// <param name="other">The other instance to compare with.</param>
    /// <returns><c>true</c> when <paramref name="other"/> is not null and has the same <see cref="Status"/>; otherwise <c>false</c>.</returns>
    public bool Equals(JobConditionStatus? other)
    {
        return other is not null && Status == other.Status;
    }

    /// <summary>
    /// Returns a hash code for this instance based on the <see cref="Status"/>.
    /// </summary>
    /// <returns>An integer hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return Status.GetHashCode();
    }

    /// <summary>
    /// Returns a string representation of the condition status and its message.
    /// </summary>
    /// <returns>A string in the format "{Status}: {Message}".</returns>
    public override string? ToString()
    {
        return $"{Status}: {Message}";
    }
}
