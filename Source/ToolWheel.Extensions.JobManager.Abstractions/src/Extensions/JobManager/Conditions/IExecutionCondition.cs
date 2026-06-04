using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Conditions;

/// <summary>
/// Represents a condition that is evaluated prior to job execution.
/// Implementations use the provided <see cref="ExecutionConditionContext"/> to indicate
/// whether the job is ready to run and optionally provide a message.
/// </summary>
public interface IExecutionCondition
{
    /// <summary>
    /// Evaluates the condition asynchronously for the job exposed by <paramref name="context"/>.
    /// Use the <paramref name="context"/> to set the resulting status via
    /// <see cref="ExecutionConditionContext.SetReady"/> or <see cref="ExecutionConditionContext.SetNotReady"/>.
    /// </summary>
    /// <param name="context">
    /// A context instance used to construct the <see cref="JobConditionStatus"/>.
    /// Callers will invoke <see cref="ExecutionConditionContext.BuildConditionStatus"/> after this method completes.
    /// </param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EvaluateAsync(ExecutionConditionContext context, CancellationToken cancellationToken);
}
