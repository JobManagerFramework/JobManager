using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToolWheel.Extensions.JobManager.Conditions;
using ToolWheel.Extensions.JobManager.Middleware;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service responsible for executing jobs by building and running an execution pipeline.
/// The service creates a scoped service provider for each job task, runs configured execution middleware,
/// invokes the target method and publishes lifecycle events (started, canceled, failed, succeeded).
/// </summary>
public partial class JobTaskExecutionService : IJobTaskExecutionService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<JobTaskExecutionService> logger;
    private readonly IJobExecutionEvaluatorService executionEvaluator;
    private readonly IJobTaskJournalService journalService;

    /// <summary>
    /// Initializes a new instance of <see cref="JobTaskExecutionService"/>.
    /// </summary>
    /// <param name="serviceScopeFactory">Factory for creating service scopes. Must not be <c>null</c>.</param>
    /// <param name="logger">Logger used to record diagnostic information. Must not be <c>null</c>.</param>
    /// <param name="executionEvaluator">Evaluator that checks execution conditions before task dispatch.</param>
    /// <param name="journalService">Service that records journal entries during job execution. Must not be <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is <c>null</c>.</exception>
    public JobTaskExecutionService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<JobTaskExecutionService> logger,
        IJobExecutionEvaluatorService executionEvaluator,
        IJobTaskJournalService journalService)
    {
        this.serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.executionEvaluator = executionEvaluator ?? throw new ArgumentNullException(nameof(executionEvaluator));
        this.journalService = journalService ?? throw new ArgumentNullException(nameof(journalService));
    }

    /// <summary>
    /// Starts execution of the specified <paramref name="job"/> and returns a runtime handle (<see cref="IJobTask"/>).
    /// Execution conditions are evaluated asynchronously before starting the background task to prevent race conditions
    /// (e.g. two jobs in the same group starting simultaneously).
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the <see cref="IJobTask"/> representing the started execution.</returns>
    public async ValueTask<IJobTask> ExecuteAsync(IJob job, CancellationToken cancellationToken = default)
    {
        var jobTask = new JobTask(job, Guid.NewGuid().ToString());

        logger.LogInformation("Starting job task {JobTaskId} for job {JobId}", jobTask.Id, job.Id);

        // If the job is explicitly disabled, short-circuit and mark as not ready.
        if (!job.Enabled)
        {
            var disabledMessage = "Job is disabled.";
            logger.LogWarning("Job {JobId} is disabled. Task {JobTaskId} will not execute.", job.Id, jobTask.Id);
            jobTask.Status = JobTaskStatusEnum.NotReady;
            jobTask.Result = new JobConditionStatus(JobConditionStatusEnum.NotReady, disabledMessage);
            return jobTask;
        }

        // Evaluate execution conditions BEFORE starting the background task.
        // This prevents race conditions where multiple jobs in the same group
        // pass condition checks because none of them has been registered yet.
        var conditionStatus = await executionEvaluator.EvaluateAsync(job, cancellationToken).ConfigureAwait(false);

        if (conditionStatus.Status == JobConditionStatusEnum.NotReady)
        {
            logger.LogWarning("Job {JobId} blocked by condition: {Reason}. Task {JobTaskId} will not execute.", job.Id, conditionStatus.Message, jobTask.Id);
            jobTask.Status = JobTaskStatusEnum.NotReady;
            jobTask.Result = conditionStatus;
            return jobTask;
        }

        // Allow Task.Run to observe the task's cancellation token so the task itself can be cancelled.
        var token = jobTask.CancellationToken?.Token ?? CancellationToken.None;

        jobTask.Task = Task.Run(async () =>
        {
            try
            {
                jobTask.StartTimestamp = DateTime.UtcNow;

                await PrepareExecution(jobTask).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during execution of job task {JobTaskId}", jobTask.Id);
                jobTask.Status = JobTaskStatusEnum.Failed;
                jobTask.Result = ex;
            }
            finally
            {
                jobTask.EndTimestamp = DateTime.UtcNow;
            }

        }, token);

        return jobTask;
    }

    /// <summary>
    /// Creates a <see cref="JobTaskJournal"/> for the given job task.
    /// If the job has a <see cref="IJob.JobLogger"/> configured, it is used as the inner logger
    /// so journal entries are forwarded to it. Otherwise the application logger is used as fallback.
    /// </summary>
    /// <param name="jobTask">The job task for which the journal is created.</param>
    /// <returns>A <see cref="JobTaskJournal"/> that records lifecycle entries and forwards them to a job-specific logger or the application logger.</returns>
    private JobTaskJournal CreateJournal(JobTask jobTask)
    {
        return new JobTaskJournal(journalService, jobTask, jobTask.Job.JobLogger ?? logger);
    }

    /// <summary>
    /// Prepares and runs the execution pipeline for the given <paramref name="jobTask"/>.
    /// This method resolves middleware, runs the pipeline and invokes the target method if appropriate.
    /// </summary>
    /// <param name="jobTask">The job task to prepare and execute.</param>
    private async Task PrepareExecution(JobTask jobTask)
    {
        // Create the journal logger and attach it to the execution pipeline.
        var journal = CreateJournal(jobTask);

        // Record lifecycle start in journal.
        journal.LogInformation("Job task {JobTaskId} started for job {JobId}", jobTask.Id, jobTask.Job.Id);

        using var scope = serviceScopeFactory.CreateScope();
        using var loggerScope = logger.BeginScope(jobTask);
        var provider = scope.ServiceProvider;
        var jobTaskContext = new JobTaskContext(jobTask);
        var jobTaskContextBuilder = new JobTaskContextBuilder(jobTaskContext);

        // Resolve middlewares from the real provider (do not intercept these)
        var executionMiddlewares = provider.GetServices<IExecutionMiddleware>().ToArray();

        if (executionMiddlewares.Length > 0)
        {
            logger.LogDebug("Executing {MiddlewareCount} middleware(s)", executionMiddlewares.Length);
        }

        // Wrap provider to make the journal available via ILogger and ILogger<T>
        var journalAwareProvider = new JobTaskJournalAwareServiceProvider(provider, journal);

        // Make the journal-aware provider available to middleware (e.g. for target object creation)
        jobTaskContextBuilder.ServiceProvider = journalAwareProvider;

        var pipeline = BuildMiddlewarePipeline(jobTask, jobTaskContextBuilder, executionMiddlewares, journalAwareProvider, jobTaskContext);

        // Execute complete pipeline including target method invocation
        await ExecutePipelineAsync(pipeline, jobTask, journal).ConfigureAwait(false);

        // Record lifecycle end in journal.
        journal.LogInformation("Job task {JobTaskId} finished with status: {Status}", jobTask.Id, jobTask.Status);

        logger.LogInformation("Execution finished with status: {Status}", jobTask.Status);
    }

    /// <summary>
    /// Builds the middleware pipeline delegate that when invoked will execute all middlewares in sequence,
    /// followed by the target method invocation.
    /// </summary>
    /// <param name="jobTask">The job task for which the pipeline is built.</param>
    /// <param name="jobTaskContextBuilder">The context builder provided to middlewares.</param>
    /// <param name="executionMiddlewares">Array of middlewares to include in the pipeline.</param>
    /// <param name="provider">Service provider for target method invocation.</param>
    /// <param name="jobTaskContext">The job task context for target method invocation.</param>
    /// <returns>A delegate representing the complete pipeline.</returns>
    private Func<Task> BuildMiddlewarePipeline(JobTask jobTask, IJobTaskContextBuilder jobTaskContextBuilder, IExecutionMiddleware[] executionMiddlewares, IServiceProvider provider, JobTaskContext jobTaskContext)
    {
        // The innermost delegate: invoke the target method
        Func<Task> pipeline = async () =>
        {
            // Check condition status before invoking target method
            if (jobTaskContextBuilder.ConditionStatus.Status == JobConditionStatusEnum.NotReady)
            {
                jobTask.Status = JobTaskStatusEnum.NotReady;
                logger.LogWarning("Skipped: condition not ready ({Reason})", jobTaskContextBuilder.ConditionStatus.Message);
                return;
            }

            await InvokeTargetMethodAsync(jobTask, provider, jobTaskContext).ConfigureAwait(false);
        };

        // BuildConditionStatus asynchronous middlewares in reverse order.
        for (int i = executionMiddlewares.Length - 1; i >= 0; i--)
        {
            var middleware = executionMiddlewares[i];
            var next = pipeline;
            pipeline = () =>
            {
                var token = jobTask.CancellationToken?.Token ?? CancellationToken.None;
                return middleware.InvokeAsync(jobTaskContextBuilder, next, token);
            };
        }

        return pipeline;
    }

    /// <summary>
    /// Executes the complete pipeline and handles cancellation and errors.
    /// Lifecycle events (canceled, failed) are recorded in the journal.
    /// </summary>
    /// <param name="pipeline">The pipeline delegate to execute.</param>
    /// <param name="jobTask">The job task being executed.</param>
    /// <param name="journal">The journal that records lifecycle entries for this execution.</param>
    private async Task ExecutePipelineAsync(Func<Task> pipeline, JobTask jobTask, JobTaskJournal journal)
    {
        try
        {
            await pipeline().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            jobTask.Status = JobTaskStatusEnum.Canceled;
            logger.LogInformation("Canceled during execution");
            journal.LogInformation("Job task {JobTaskId} canceled during execution", jobTask.Id);
        }
        catch (Exception ex)
        {
            jobTask.Result = ex;
            jobTask.Status = JobTaskStatusEnum.Failed;
            logger.LogError(ex, "Failed during execution");
            journal.LogError(ex, "Job task {JobTaskId} failed during execution", jobTask.Id);
        }
    }

    private async Task InvokeTargetMethodAsync(JobTask jobTask, IServiceProvider provider, JobTaskContext jobTaskContext)
    {
        var parameterInfos = jobTask.Job.TargetMethod.GetParameters();
        var paramCount = parameterInfos.Length;
        // Use ArrayPool instead of ToArray() to reduce GC pressure.
        var argsArray = ArrayPool<object?>.Shared.Rent(paramCount);

        // Set status to running at the beginning of execution.
        jobTask.Status = JobTaskStatusEnum.Running;

        try
        {
            int idx = 0;
            foreach (var value in ResolveParameters(jobTask, provider, jobTaskContext))
            {
                if (idx >= paramCount) break;
                argsArray[idx++] = value;
            }

            try
            {
                logger.LogDebug("Executing method {Method}", jobTask.Job.TargetMethod.Name);

                // Use MethodInfo.Invoke to create a reflection boundary.
                // This ensures that exceptions thrown by the target method trigger a first-chance exception
                // at the original throw site, allowing the debugger to break at the correct location.
                var invokeResult = jobTask.Job.TargetMethod.Invoke(jobTaskContext.TargetObject, argsArray[..paramCount]);

                jobTask.Result = await AwaitTaskLikeAsync(invokeResult).ConfigureAwait(false);

                if (jobTask.Status != JobTaskStatusEnum.Canceled && jobTask.Status != JobTaskStatusEnum.Failed)
                {
                    jobTask.Status = JobTaskStatusEnum.Success;
                    logger.LogDebug("Completed successfully");
                }
            }
            catch (TargetInvocationException tie) when (tie.InnerException is OperationCanceledException)
            {
                jobTask.Status = JobTaskStatusEnum.Canceled;
                logger.LogDebug("Canceled");
            }
            catch (TargetInvocationException tie) when (tie.InnerException is not null)
            {
                jobTask.Result = tie.InnerException;
                jobTask.Status = JobTaskStatusEnum.Failed;
                logger.LogError(tie.InnerException, "Failed with exception");
            }
            catch (OperationCanceledException)
            {
                jobTask.Status = JobTaskStatusEnum.Canceled;
                logger.LogDebug("Canceled");
            }
            catch (Exception ex)
            {
                jobTask.Result = ex;
                jobTask.Status = JobTaskStatusEnum.Failed;
                logger.LogError(ex, "Failed with unexpected exception");
            }
        }
        finally
        {
            // Clear references before returning — important to avoid leaks for large arrays.
            for (int i = 0; i < paramCount; i++)
            {
                argsArray[i] = null;
            }

            ArrayPool<object?>.Shared.Return(argsArray);
        }
    }

    /// <summary>
    /// Resolves parameters for a target method by inspecting parameter types and using the scoped service provider or context values.
    /// </summary>
    /// <param name="jobTask">The job task being executed.</param>
    /// <param name="provider">The scoped <see cref="IServiceProvider"/> used to resolve services.</param>
    /// <param name="jobTaskContext">The job task context provided to the invocation.</param>
    /// <returns>An enumerable of parameter values in the same order as the target method signature.</returns>
    private IEnumerable<object?> ResolveParameters(JobTask jobTask, IServiceProvider provider, JobTaskContext jobTaskContext)
    {
        foreach (var parameter in jobTask.Job.TargetMethod.GetParameters())
        {
            if (parameter.ParameterType == typeof(IJobTask))
            {
                yield return jobTask;
            }
            else if (parameter.ParameterType == typeof(CancellationToken))
            {
                yield return jobTask.CancellationToken?.Token ?? CancellationToken.None;
            }
            else if (parameter.ParameterType == typeof(IJobTaskContext))
            {
                yield return jobTaskContext;
            }
            else
            {
                var service = provider.GetService(parameter.ParameterType);
                if (service is null)
                {
                    // If service cannot be resolved: prefer default value if present; otherwise null.
                    if (parameter.HasDefaultValue)
                    {
                        yield return parameter.DefaultValue;
                    }
                    else
                    {
                        logger.LogWarning("Could not resolve service for parameter {ParameterName} ({ParameterType})",
                            parameter.Name, parameter.ParameterType.Name);
                        yield return null;
                    }
                }
                else
                {
                    yield return service;
                }
            }
        }
    }

    /// <summary>
    /// Awaits a task-like return value (Task, Task&lt;T&gt;, ValueTask, ValueTask&lt;T&gt;) and returns the result if any.
    /// For synchronous results the value is returned directly.
    /// </summary>
    /// <param name="invokeResult">The value returned by the method invoker.</param>
    /// <returns>The awaited result for Task-like values or the original value for synchronous results.</returns>
    private static async Task<object?> AwaitTaskLikeAsync(object? invokeResult)
    {
        if (invokeResult is null)
        {
            return null;
        }

        // Task or Task<T>
        if (invokeResult is Task task)
        {
            await task.ConfigureAwait(false);

            var taskType = task.GetType();
            if (taskType.IsGenericType && taskType.GetProperty("Result") is PropertyInfo resultProp)
            {
                return resultProp.GetValue(task);
            }

            return null;
        }

        // ValueTask or ValueTask<T> (convert via AsTask)
        var type = invokeResult.GetType();
        var fullName = type.FullName ?? string.Empty;
        if (fullName.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal))
        {
            var asTask = type.GetMethod("AsTask", BindingFlags.Instance | BindingFlags.Public);
            if (asTask != null)
            {
                var asTaskObj = asTask.Invoke(invokeResult, null) as Task;
                if (asTaskObj != null)
                {
                    await asTaskObj.ConfigureAwait(false);

                    var taskType = asTaskObj.GetType();
                    if (taskType.IsGenericType && taskType.GetProperty("Result") is PropertyInfo resultProp)
                    {
                        return resultProp.GetValue(asTaskObj);
                    }

                    return null;
                }
            }
        }

        // Otherwise: synchronous result (e.g. int, object)
        return invokeResult;
    }
}
