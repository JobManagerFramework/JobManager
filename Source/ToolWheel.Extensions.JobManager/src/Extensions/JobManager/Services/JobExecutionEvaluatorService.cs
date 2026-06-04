using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToolWheel.Extensions.JobManager.Conditions;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Default implementation of <see cref="IJobExecutionEvaluatorService"/> that iterates over all
/// registered <see cref="IExecutionConditionController"/> instances and aggregates their results.
/// Evaluation short-circuits as soon as any controller indicates <see cref="JobConditionStatusEnum.NotReady"/>.
/// </summary>
public class JobExecutionEvaluatorService : IJobExecutionEvaluatorService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<JobExecutionEvaluatorService> logger;

    /// <summary>
    /// Initializes a new instance of <see cref="JobExecutionEvaluatorService"/>.
    /// </summary>
    /// <param name="serviceProvider">The application service provider used to resolve condition controllers.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is <c>null</c>.</exception>
    public JobExecutionEvaluatorService(
        IServiceProvider serviceProvider,
        ILogger<JobExecutionEvaluatorService> logger)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask<JobConditionStatus> EvaluateAsync(IJob job, CancellationToken cancellationToken = default)
    {
        var executionConditionControllers = serviceProvider.GetServices<IExecutionConditionController>();
        var builder = new ExecutionConditionContext(job, DateTime.UtcNow); //TODO: Hier noch das Signaldate übergeben, sobald das in der JobExecutionService verfügbar ist

        foreach (var controller in executionConditionControllers)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await controller.EvaluateAsync(builder, cancellationToken).ConfigureAwait(false);

                logger.LogDebug("Condition controller {Controller} evaluated job {JobId}", controller.GetType().FullName, job.Id);

                if (builder.Status == JobConditionStatusEnum.NotReady)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Condition controller {Controller} threw an exception while evaluating job {JobId}",
                    controller.GetType().FullName, job.Id);
            }
        }

        var status = builder.BuildConditionStatus();

        logger.LogInformation("Evaluation of job {JobId} completed with status {Status} : {Message}", job.Id, status.Status, status.Message);

        return status;
    }
}

