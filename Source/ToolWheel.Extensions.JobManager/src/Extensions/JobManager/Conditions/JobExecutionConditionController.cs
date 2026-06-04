using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager.Conditions;

/// <summary>
/// Coordinates the evaluation of registered execution conditions for a job.
/// Iterates through configured <see cref="IExecutionCondition"/> instances and uses
/// a <see cref="ExecutionConditionContext"/> to aggregate the result.
/// </summary>
public class JobExecutionConditionController : IExecutionConditionController
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<JobExecutionConditionController> logger;

    /// <summary>
    /// Initializes a new instance of <see cref="JobExecutionConditionController"/>.
    /// </summary>
    /// <param name="serviceProvider">Service provider used to lazily resolve execution conditions.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is <c>null</c>.</exception>
    public JobExecutionConditionController(IServiceProvider serviceProvider, ILogger<JobExecutionConditionController> logger)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Evaluates all configured execution conditions asynchronously for the job exposed by <paramref name="context"/>
    /// and sets the result via the provided context.
    /// Evaluation stops early if any condition indicates not-ready.
    /// </summary>
    /// <param name="context">The context used to construct the aggregated condition result.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public async ValueTask EvaluateAsync(ExecutionConditionContext context, CancellationToken cancellationToken)
    {
        var executionConditions = serviceProvider.GetServices<IExecutionCondition>();

        logger.LogDebug("Evaluating conditions for job {JobId}", context.Job.Id);

        foreach (var condition in executionConditions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await condition.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Condition {Condition} evaluated job {JobId}. Status: {Status}", condition.GetType().FullName, context.Job.Id, context.Status);

            // Wenn NotReady, dann direkt verlassen.
            if (context.Status == JobConditionStatusEnum.NotReady)
            {
                break;
            }
        }

        logger.LogDebug("All conditions evaluated for job {JobId}", context.Job.Id);
    }
}
