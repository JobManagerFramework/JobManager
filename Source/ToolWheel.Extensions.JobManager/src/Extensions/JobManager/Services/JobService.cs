using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Storage;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service responsible for registering, querying and executing jobs.
/// Delegates persistence to <see cref="IJobStorage"/> and execution to <see cref="IJobTaskService"/>.
/// </summary>
public class JobService : IJobService
{
    private readonly IJobStorage jobStorage;
    private readonly IJobTaskService jobTaskService;
    private readonly ILogger<JobService> logger;
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="JobService"/>.
    /// </summary>
    /// <param name="logger">An <see cref="ILogger{JobService}"/> for logging.</param>
    /// <param name="jobTaskService">The <see cref="IJobTaskService"/> used to execute jobs.</param>
    /// <param name="serviceProvider">The application service provider used to resolve feature services during job registration.</param>
    /// <param name="jobStorage">The <see cref="IJobStorage"/> used to persist job registrations.</param>
    public JobService(ILogger<JobService> logger, IJobTaskService jobTaskService, IServiceProvider serviceProvider, IJobStorage jobStorage)
    {
        this.jobTaskService = jobTaskService;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.jobStorage = jobStorage;

        this.logger.LogDebug("JobService created.");
    }

    /// <summary>
    /// Adds a new job to the registry based on the provided <paramref name="jobDescription"/>.
    /// </summary>
    /// <param name="jobDescription">The description that represents the job to add.</param>
    /// <returns>The created <see cref="IJob"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a job with the same id already exists.</exception>
    public IJob Add(IJobDescription jobDescription)
    {
        logger.LogDebug("Attempting to add job with Id '{JobId}' and Target '{TargetObject}.{TargetMethod}'", jobDescription.Id, jobDescription.TargetObject, jobDescription.TargetMethod);

        var job = new Job(jobDescription.Id, jobDescription.TargetObject, jobDescription.TargetMethod, jobDescription.UseSingletonInstance)
        {
            Name = jobDescription.Name,
            Description = jobDescription.Description,
            Enabled = jobDescription.Enabled,
            JobLogger = jobDescription.JobLogger
        };

        if (!jobStorage.TryAdd(job))
        {
            logger.LogWarning("A job with the id '{JobId}' already exists. Add operation aborted.", jobDescription.Id);
            throw new InvalidOperationException($"A job with the id '{jobDescription.Id}' already exists.");
        }

        foreach (var feature in jobDescription.Features)
        {
            feature.Apply(serviceProvider, jobDescription, job);
        }

        logger.LogInformation("Job added with Id '{JobId}', Name '{JobName}'", job.Id, job.Name);

        return job;
    }

    /// <summary>
    /// Updates an existing job in the registry, replacing it with <paramref name="updateJob"/>.
    /// </summary>
    /// <param name="updateJob">The job instance containing updated values. The <see cref="Job.Id"/> must match an existing job.</param>
    /// <exception cref="InvalidOperationException">Thrown when no job with the given id is registered.</exception>
    public void Update(IJob updateJob)
    {
        if (updateJob is not Job jobRecord)
        {
            throw new InvalidOperationException($"Job with id '{updateJob.Id}' is not a valid Job instance.");
        }

        var currentJob = jobStorage.FindById(updateJob.Id)
            ?? throw new InvalidOperationException($"No job found with id '{updateJob.Id}' to update.");

        jobStorage.TryUpdate(updateJob.Id, jobRecord, currentJob);
    }

    /// <inheritdoc />
    public void Update(string jobId, string name, string description, bool enabled)
    {
        var currentJob = jobStorage.FindById(jobId) as Job
            ?? throw new InvalidOperationException($"No job found with id '{jobId}' to update.");

        var updated = currentJob with
        {
            Name = name,
            Description = description,
            Enabled = enabled
        };

        jobStorage.TryUpdate(jobId, updated, currentJob);
    }

    /// <summary>
    /// Removes the specified job from the registry.
    /// </summary>
    /// <param name="job">The job instance to remove.</param>
    /// <returns><c>true</c> if the job was removed; otherwise <c>false</c>.</returns>
    public bool Remove(IJob job)
    {
        logger.LogDebug("Removing job with Id '{JobId}'", job.Id);

        bool removed = jobStorage.TryRemove(job.Id);

        if (removed)
        {
            logger.LogInformation("Removed job with Id '{JobId}'", job.Id);
        }
        else
        {
            logger.LogWarning("Attempted to remove job with Id '{JobId}' but it was not found", job.Id);
        }

        return removed;
    }

    /// <summary>
    /// Attempts to find a job by its identifier.
    /// </summary>
    /// <param name="jobId">The identifier of the job to locate.</param>
    /// <returns>The matching <see cref="IJob"/> if found; otherwise <c>null</c>.</returns>
    public IJob? FindById(string jobId)
    {
        logger.LogDebug("Looking up job by Id '{JobId}'", jobId);

        var job = jobStorage.FindById(jobId);

        if (job is null)
        {
            logger.LogDebug("No job found with Id '{JobId}'", jobId);
        }
        else
        {
            logger.LogDebug("Found job with Id '{JobId}', Name '{JobName}'", job.Id, job.Name);
        }

        return job;
    }

    /// <summary>
    /// Reads a job by its identifier and throws if it does not exist.
    /// </summary>
    /// <param name="jobId">The identifier of the job to read.</param>
    /// <returns>The matching <see cref="IJob"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no job with <paramref name="jobId"/> exists.</exception>
    public IJob ReadById(string jobId)
    {
        logger.LogDebug("Reading job by Id '{JobId}'", jobId);

        var job = FindById(jobId);

        if (job is null)
        {
            logger.LogWarning("No job found with id '{JobId}' when calling ReadById", jobId);
            throw new KeyNotFoundException($"No job found with id '{jobId}'.");
        }

        return job;
    }

    /// <summary>
    /// Returns all registered jobs.
    /// </summary>
    /// <returns>An enumerable of <see cref="IJob"/> instances.</returns>
    public IEnumerable<IJob> ReadAll()
    {
        logger.LogDebug("Reading all jobs.");
        return jobStorage.GetAll();
    }

    /// <summary>
    /// Executes the specified job by delegating to the configured <see cref="IJobTaskService"/>.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the <see cref="IJobTask"/> representing the started execution.</returns>
    public ValueTask<IJobTask> ExecuteAsync(IJob job, CancellationToken cancellationToken = default)
    {
        return jobTaskService.ExecuteAsync(job, cancellationToken);
    }
}
