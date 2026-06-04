using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ToolWheel.Extensions.JobManager;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Services;

namespace ToolWheel;

/// <summary>
/// Provides extension methods for adding jobs to an <see cref="IJobService"/> instance.
/// </summary>
/// <remarks>
/// Extension helpers simplify creating <see cref="JobDescription"/> instances from delegates
/// or expression trees and add them to the configured <see cref="IJobService"/>.
/// </remarks>
public static class JobServiceExtensions
{
    /// <summary>
    /// Creates a <see cref="JobDescription"/> for the given delegate and adds it to the <paramref name="jobService"/>.
    /// </summary>
    /// <param name="jobService">The job service to add the job to.</param>
    /// <param name="targetDelegate">The delegate that represents the method to be executed by the job.</param>
    /// <param name="jobBuilder">Optional action to configure the created <see cref="JobDescription"/>.</param>
    /// <returns>The created and registered <see cref="IJob"/> instance.</returns>
    public static IJob Add(this IJobService jobService, Delegate targetDelegate, Action<IJobDescriptionBuilder>? jobBuilder = null)
    {
        var targetObject = targetDelegate.Target;
        var targetMethod = targetDelegate.Method;
        var jobDescription = new JobDescription(targetObject, targetMethod);
        var jobDescriptionBuilder = new JobDescriptionBuilder(jobDescription);

        jobBuilder?.Invoke(jobDescriptionBuilder);

        return jobService.Add(jobDescription);  
    }

    /// <summary>
    /// Resolves a method call from an expression and creates a <see cref="JobDescription"/> which is then added to the <paramref name="jobService"/>.
    /// </summary>
    /// <typeparam name="T">The target type that contains the method in the expression.</typeparam>
    /// <param name="jobService">The job service to add the job to.</param>
    /// <param name="targetExpression">An expression that resolves to a delegate invocation for the target method.</param>
    /// <param name="jobBuilder">Optional action to configure the created <see cref="JobDescription"/>.</param>
    /// <returns>The created and registered <see cref="IJob"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method could not be resolved from the provided expression.</exception>
    public static IJob Add<T>(this IJobService jobService, Expression<Func<T, Delegate>> targetExpression, Action<IJobDescriptionBuilder>? jobBuilder = null)
        where T : class
    {
        var targetMethod = JobManagerMethodExpressionResolver.ResolveMethodCall(targetExpression) ?? throw new InvalidOperationException("Could not resolve method from expression.");
        var jobDescription = new JobDescription(null, targetMethod);
        var jobDescriptionBuilder = new JobDescriptionBuilder(jobDescription);

        jobBuilder?.Invoke(jobDescriptionBuilder);

        return jobService.Add(jobDescription);
    }
}
