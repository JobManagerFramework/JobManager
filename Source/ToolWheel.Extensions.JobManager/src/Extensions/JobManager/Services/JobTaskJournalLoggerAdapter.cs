using System;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Generic adapter that implements <see cref="ILogger{TCategory}"/> by forwarding all calls
/// to a non-generic <see cref="ILogger"/> instance (e.g. the <see cref="JobTaskJournal"/>).
/// </summary>
/// <typeparam name="TCategory">The category type used to identify the logger.</typeparam>
internal sealed class JobTaskJournalLoggerAdapter<TCategory> : ILogger<TCategory>
{
    private readonly ILogger inner;

    /// <summary>
    /// Initializes a new instance of <see cref="JobTaskJournalLoggerAdapter{TCategory}"/>.
    /// </summary>
    /// <param name="inner">The inner logger to forward all calls to. Must not be <c>null</c>.</param>
    public JobTaskJournalLoggerAdapter(ILogger inner)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return inner.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return inner.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
