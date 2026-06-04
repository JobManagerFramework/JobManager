using System;
using Microsoft.Extensions.Logging;
using ToolWheel.Extensions.JobManager.Services;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// An <see cref="ILogger"/> implementation that routes all log entries produced during a
/// single job task execution to the <see cref="IJobTaskJournalService"/>.
/// Optionally forwards every entry to an inner logger as well.
/// All operations are thread-safe.
/// </summary>
public sealed class JobTaskJournal : ILogger
{
    private readonly IJobTaskJournalService journalService;
    private readonly IJobTask jobTask;
    private readonly ILogger? innerLogger;

    /// <summary>
    /// Initializes a new instance of <see cref="JobTaskJournal"/>.
    /// </summary>
    /// <param name="journalService">The service that stores journal entries. Must not be <c>null</c>.</param>
    /// <param name="jobTask">The job task for which entries are recorded. Must not be <c>null</c>.</param>
    /// <param name="innerLogger">
    /// An optional inner logger to which all log calls are forwarded in addition to being recorded.
    /// Pass <c>null</c> to only record entries without forwarding.
    /// </param>
    public JobTaskJournal(IJobTaskJournalService journalService, IJobTask jobTask, ILogger? innerLogger = null)
    {
        this.journalService = journalService ?? throw new ArgumentNullException(nameof(journalService));
        this.jobTask = jobTask ?? throw new ArgumentNullException(nameof(jobTask));
        this.innerLogger = innerLogger;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return innerLogger?.BeginScope(state);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        var message = formatter(state, exception);

        journalService.Append(jobTask, new JobTaskJournalEntry(
            DateTimeOffset.UtcNow,
            logLevel,
            message,
            exception));

        // Forward to the inner logger if available.
        innerLogger?.Log(logLevel, eventId, state, exception, formatter);
    }
}
