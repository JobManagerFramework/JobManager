using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Conditions;

/// <summary>
/// Controller responsible for evaluating conditions that affect whether a job may execute.
/// Implementations coordinate the evaluation logic and update the provided <see cref="ExecutionConditionContext"/> accordingly.
/// </summary>
public interface IExecutionConditionController
{
    /// <summary>
    /// Evaluates condition(s) asynchronously for the job exposed by <paramref name="context"/>
    /// and sets the result via the provided context.
    /// </summary>
    /// <param name="context">
    /// A context instance used to construct the <see cref="JobConditionStatus"/>.
    /// Callers will invoke <see cref="ExecutionConditionContext.BuildConditionStatus"/> after this method completes.
    /// </param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask EvaluateAsync(ExecutionConditionContext context, CancellationToken cancellationToken);
}
