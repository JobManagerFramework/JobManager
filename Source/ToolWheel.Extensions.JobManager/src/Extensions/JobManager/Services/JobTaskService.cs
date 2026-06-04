using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ToolWheel.Extensions.JobManager.Storage;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Provides execution and tracking for job tasks.
/// Delegates persistence to <see cref="IJobTaskStorage"/>.
/// </summary>
public class JobTaskService : IJobTaskService
{
    private readonly IJobTaskExecutionService jobTaskExecutionService;
    private readonly IJobTaskStorage jobTaskStorage;
    private readonly IJobTaskJournalService journalService;
    private readonly ILogger<JobTaskService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobTaskService"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="jobTaskExecutionService">Service that executes jobs. Must not be <c>null</c>.</param>
    /// <param name="jobTaskStorage">The <see cref="IJobTaskStorage"/> used to track job tasks. Must not be <c>null</c>.</param>
    /// <param name="journalService">Service that records journal entries for job tasks. Must not be <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependency is <c>null</c>.</exception>
    public JobTaskService(ILogger<JobTaskService> logger, IJobTaskExecutionService jobTaskExecutionService, IJobTaskStorage jobTaskStorage, IJobTaskJournalService journalService)
    {
        this.jobTaskExecutionService = jobTaskExecutionService ?? throw new ArgumentNullException(nameof(jobTaskExecutionService));
        this.jobTaskStorage = jobTaskStorage ?? throw new ArgumentNullException(nameof(jobTaskStorage));
        this.journalService = journalService ?? throw new ArgumentNullException(nameof(journalService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.logger.LogDebug("JobTaskService created.");
    }

    /// <summary>
    /// Executes the specified <paramref name="job"/> by delegating to <see cref="IJobTaskExecutionService"/> and tracks the returned task.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the created and tracked <see cref="IJobTask"/> instance.</returns>
    public async ValueTask<IJobTask> ExecuteAsync(IJob job, CancellationToken cancellationToken = default)
    {
        var jobTask = await jobTaskExecutionService.ExecuteAsync(job, cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Enqueuing task {TaskId} for job {JobId}", jobTask.Id, job.Id);

        jobTaskStorage.Add(job, jobTask);

        return jobTask;
    }

    /// <summary>
    /// Returns all currently known or tracked job tasks.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{IJobTask}"/> that enumerates all job tasks.</returns>
    public IEnumerable<IJobTask> ReadAll()
    {
        logger.LogDebug("Reading all job tasks.");

        return jobTaskStorage.GetAll();
    }

    /// <summary>
    /// Requests cancellation for all currently tracked job tasks and waits until they have completed.
    /// Implementations signal cancellation on each task's <see cref="IJobTask.CancellationToken"/> and then await their underlying <see cref="IJobTask.Task"/> instances.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the wait for tasks to complete.</param>
    /// <returns>A task that completes when all tracked job tasks have finished or the <paramref name="cancellationToken"/> is triggered.</returns>
    public async Task CancelAllAndWaitAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cancelling all running job tasks and waiting for completion.");

        var tasks = new List<Task>();

        foreach (var jt in jobTaskStorage.GetAll())
        {
            if (jt.Status == JobTaskStatusEnum.Pending || jt.Status == JobTaskStatusEnum.Running)
            {
                journalService.Append(jt, new JobTaskJournalEntry(
                    DateTimeOffset.UtcNow,
                    LogLevel.Warning,
                    $"Application shutdown requested. Job task {jt.Id} cancellation initiated.",
                    null));
            }

            try
            {
                jt.CancellationToken?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Task's cancellation token source already disposed - ignore
            }

            if (jt.Task != null)
            {
                tasks.Add(jt.Task);
            }
        }

        try
        {
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Waiting for job tasks was canceled by token.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occurred while waiting for job tasks to complete.");
        }
    }

    /// <summary>
    /// Returns all job tasks associated with the specified <paramref name="job"/>.
    /// </summary>
    /// <param name="job">The job whose tasks should be returned.</param>
    /// <returns>An <see cref="IEnumerable{IJobTask}"/> that enumerates the tasks for the given job.</returns>
    public IEnumerable<IJobTask> ReadByJob(IJob job)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        logger.LogDebug("Reading tasks for job {JobId}", job.Id);

        return jobTaskStorage.GetByJob(job);
    }

    /// <summary>
    /// Retrieves all job tasks for <paramref name="job"/> that match any of the provided statuses.
    /// </summary>
    /// <param name="job">The job for which to retrieve associated tasks. Cannot be null.</param>
    /// <param name="status">An optional array of task statuses to filter the results.
    /// If no statuses are provided, all tasks for the job are returned.</param>
    /// <returns>An enumerable collection of job tasks that belong to the specified job and match any of the given statuses.
    /// Returns an empty collection if no matching tasks are found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public IEnumerable<IJobTask> ReadByJob(IJob job, params JobTaskStatusEnum[] status)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        var statusCount = status?.Length ?? 0;
        logger.LogDebug("Reading tasks for job {JobId} with statuses count {StatusCount}", job.Id, statusCount);

        if (statusCount == 0)
        {
            return jobTaskStorage.GetByJob(job);
        }

        return jobTaskStorage.GetByJob(job, status!);
    }

    /// <summary>
    /// Removes the specified job task from its associated job.
    /// </summary>
    /// <remarks>If the specified job task is not found in the job's task list, a warning is logged. The
    /// operation is thread-safe due to locking on the task list.</remarks>
    /// <param name="jobTask">The job task to remove from the job. This parameter cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="jobTask"/> is null.</exception>
    public void Remove(IJobTask jobTask)
    {
        if (jobTask is null)
        {
            throw new ArgumentNullException(nameof(jobTask));
        }

        logger.LogInformation("Removing task {TaskId} from job {JobId}", jobTask.Id, jobTask.Job.Id);

        var removed = jobTaskStorage.TryRemove(jobTask);

        if (!removed)
        {
            logger.LogWarning("Attempted to remove task {TaskId} which was not found for job {JobId}", jobTask.Id, jobTask.Job.Id);
        }
        else
        {
            logger.LogInformation("Task {TaskId} removed from job {JobId}", jobTask.Id, jobTask.Job.Id);
        }
    }

    /// <summary>
    /// Finds a job task by its identifier within the specified job.
    /// </summary>
    /// <param name="job">The job to search within. Cannot be <c>null</c>.</param>
    /// <param name="jobTaskId">The identifier of the task to find.</param>
    /// <returns>The matching <see cref="IJobTask"/> if found; otherwise <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> or <paramref name="jobTaskId"/> is <c>null</c>.</exception>
    public IJobTask? FindByTaskId(IJob job, string jobTaskId)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (jobTaskId is null)
        {
            throw new ArgumentNullException(nameof(jobTaskId));
        }

        logger.LogDebug("Finding task {TaskId} for job {JobId}", jobTaskId, job.Id);

        return jobTaskStorage.FindByTaskId(job, jobTaskId);
    }

    /// <summary>
    /// Requests cancellation for the specified job task by appending a journal entry
    /// and signalling its <see cref="IJobTask.CancellationToken"/>.
    /// </summary>
    /// <param name="jobTask">The job task to cancel. Cannot be <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jobTask"/> is <c>null</c>.</exception>
    public void CancelTask(IJobTask jobTask)
    {
        if (jobTask is null)
        {
            throw new ArgumentNullException(nameof(jobTask));
        }

        logger.LogInformation("Cancellation requested for task {TaskId} of job {JobId}", jobTask.Id, jobTask.Job.Id);

        journalService.Append(jobTask, new JobTaskJournalEntry(
            DateTimeOffset.UtcNow,
            LogLevel.Warning,
            $"Cancellation requested for task {jobTask.Id}.",
            null));

        try
        {
            jobTask.CancellationToken?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Cancellation token source already disposed - ignore
        }
    }
}
