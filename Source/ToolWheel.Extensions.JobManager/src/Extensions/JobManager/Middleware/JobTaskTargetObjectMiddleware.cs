using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ToolWheel.Extensions.JobManager.Middleware;

/// <summary>
/// Provides middleware for ensuring that the target object instance is available for non-static job task methods during
/// execution.
/// </summary>
/// <remarks>This middleware is intended for use in job task execution pipelines where method invocation may
/// require an instance of the declaring type. If the target method is not static and no target object is set, the
/// middleware creates an instance using the provided service provider. This enables dependency injection for job task
/// targets and supports scenarios where tasks require instance methods.</remarks>
public class JobTaskTargetObjectMiddleware : IExecutionMiddleware
{
    private readonly IServiceProvider serviceProvider;
    private readonly ConcurrentDictionary<string, object> singletonInstances = new ConcurrentDictionary<string, object>();

    /// <summary>
    /// Initializes a new instance of the JobTaskTargetObjectMiddleware class using the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies required by the middleware. Cannot be null.</param>
    public JobTaskTargetObjectMiddleware(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Invokes the next middleware delegate in the job task pipeline asynchronously, ensuring that the target object
    /// for the method is initialized if required.
    /// </summary>
    /// <remarks>If the target method is not static and the target object is not set, an instance of the
    /// declaring type is created using dependency injection before invoking the next delegate. This method is typically
    /// used within a middleware pipeline for job task execution.</remarks>
    /// <param name="context">The builder that provides context information about the job task, including the target method and object
    /// instance.</param>
    /// <param name="next">A delegate representing the next middleware or operation to execute in the pipeline.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation of invoking the next middleware delegate.</returns>
    public Task InvokeAsync(IJobTaskContextBuilder context, Func<Task> next, CancellationToken cancellationToken)
    {
        // Wenn es keine statische Methode ist, dann muss ein TargetObject vorhanden sein
        if (!context.TargetMethod.IsStatic)
        {
            // Versuche das TargetObject zu erstellen, falls es noch nicht gesetzt ist
            if (context.TargetObject == null)
            {
                // Use the context's ServiceProvider (journal-aware) so that the job class
                // receives loggers that write to the job task journal.
                var resolveProvider = context.ServiceProvider ?? serviceProvider;

                if (context.Job.UseSingletonInstance)
                {
                    // Verwende oder erstelle eine Singleton-Instanz
                    context.TargetObject = singletonInstances.GetOrAdd(
                        context.Job.Id,
                        _ => ActivatorUtilities.CreateInstance(serviceProvider, context.TargetMethod.DeclaringType!));
                }
                else
                {
                    // Erstelle eine neue Instanz für jeden Aufruf
                    context.TargetObject = ActivatorUtilities.CreateInstance(resolveProvider, context.TargetMethod.DeclaringType!);
                }
            }
        }

        return next();
    }
}
