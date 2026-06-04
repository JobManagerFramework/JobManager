using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Decorator <see cref="IServiceProvider"/> that resolves <see cref="ILogger"/> and <see cref="ILogger{T}"/>
/// from the attached <see cref="JobTaskJournal"/> and delegates all other resolutions to the inner provider.
/// This ensures that services resolved during job execution write their log entries to the task journal.
/// </summary>
internal sealed class JobTaskJournalAwareServiceProvider : IServiceProvider
{
    private readonly IServiceProvider inner;
    private readonly ILogger journal;

    /// <summary>
    /// Initializes a new instance of <see cref="JobTaskJournalAwareServiceProvider"/>.
    /// </summary>
    /// <param name="inner">The real service provider to delegate non-logger resolutions to. Must not be <c>null</c>.</param>
    /// <param name="journal">The journal logger to expose as <see cref="ILogger"/> and <see cref="ILogger{T}"/>. Must not be <c>null</c>.</param>
    public JobTaskJournalAwareServiceProvider(IServiceProvider inner, ILogger journal)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
    }

    /// <summary>
    /// Resolves the requested service. Returns the journal for <see cref="ILogger"/> and
    /// <see cref="ILogger{T}"/> requests; returns <see cref="CancellationToken.None"/> for
    /// <see cref="CancellationToken"/> requests with a warning; delegates everything else to the inner provider.
    /// </summary>
    /// <param name="serviceType">The service type to resolve.</param>
    /// <returns>The resolved service instance, or <c>null</c> if not available.</returns>
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(ILogger))
        {
            return journal;
        }

        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(ILogger<>))
        {
            var genericArg = serviceType.GetGenericArguments()[0];
            var adapterType = typeof(JobTaskJournalLoggerAdapter<>).MakeGenericType(genericArg);
            return Activator.CreateInstance(adapterType, journal);
        }

        var requestedService = inner.GetService(serviceType);
        if (requestedService is null)
        {
            journal.LogWarning("A service of type {Type} was requested via constructor injection but is not registered in the job execution Context. This may lead to runtime errors if the service is required for job execution.",
                serviceType.FullName);
        }
                
        // Delegate all other resolutions
        return requestedService;
    }
}
