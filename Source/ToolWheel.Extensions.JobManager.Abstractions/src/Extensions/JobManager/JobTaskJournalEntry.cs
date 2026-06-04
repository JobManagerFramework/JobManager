using System;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager;

/// <summary>
/// Represents a single log entry captured during the execution of a job task.
/// </summary>
/// <param name="Timestamp">The UTC timestamp when the log entry was recorded.</param>
/// <param name="LogLevel">The severity level of the log entry.</param>
/// <param name="Message">The formatted log message.</param>
/// <param name="Exception">The exception associated with the log entry, if any.</param>
public sealed record JobTaskJournalEntry(DateTimeOffset Timestamp, LogLevel LogLevel, string Message, Exception? Exception);
