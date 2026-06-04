using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Service responsible for recording and querying journal entries produced during job task executions.
/// Delegates persistence to <see cref="IJobTaskJournalStorage"/>.
/// </summary>
public class JobTaskJournalService : IJobTaskJournalService
{
    private readonly IJobTaskJournalStorage storage;
    private readonly ILogger<JobTaskJournalService> logger;

    /// <summary>
    /// Initializes a new instance of <see cref="JobTaskJournalService"/>.
    /// </summary>
    /// <param name="storage">The storage used to persist journal entries. Must not be <c>null</c>.</param>
    /// <param name="logger">Logger for diagnostic messages. Must not be <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependency is <c>null</c>.</exception>
    public JobTaskJournalService(IJobTaskJournalStorage storage, ILogger<JobTaskJournalService> logger)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Append(IJobTask jobTask, JobTaskJournalEntry entry)
    {
        storage.Append(jobTask.Id, entry);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<JobTaskJournalEntry> GetEntries(IJobTask jobTask)
    {
        logger.LogTrace("Reading journal entries for task {TaskId}", jobTask.Id);
        return storage.GetEntries(jobTask.Id);
    }
}
