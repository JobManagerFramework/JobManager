using System;
using System.Linq.Expressions;
using System.Reflection;
using ToolWheel.Extensions.JobManager.Configuration;

namespace ToolWheel
{
    /// <summary>
    /// Extension methods to simplify adding job descriptions to an <see cref="IJobDescriptionCollection"/>.
    /// </summary>
    public static class JobDescriptionCollectionExtensions
    {
        /// <summary>
        /// Adds a job description for the provided delegate and returns a builder to configure it.
        /// </summary>
        /// <param name="collection">The target job description collection.</param>
        /// <param name="id">The identifier to assign to the job description.</param>
        /// <param name="targetDelegate">The delegate that represents the target method (instance or static).</param>
        /// <returns>An <see cref="IJobDescriptionBuilder"/> to further configure the created description.</returns>
        public static IJobDescriptionBuilder Add(this IJobDescriptionCollection collection, string id, Delegate targetDelegate)
        {
            var targetObject = targetDelegate.Target;
            var targetMethod = targetDelegate.Method;

            var jobDescription = new JobDescription(targetObject, targetMethod)
            {
                Id = id
            };

            collection.Add(jobDescription);

            return new JobDescriptionBuilder(jobDescription);
        }

        /// <summary>
        /// Adds a job description for a delegate factory and returns a builder to configure it.
        /// </summary>
        /// <typeparam name="T">The factory target type.</typeparam>
        /// <param name="collection">The target job description collection.</param>
        /// <param name="id">The identifier to assign to the job description.</param>
        /// <param name="targetDelegate">A delegate factory; the method info of the delegate will be used.</param>
        /// <returns>An <see cref="IJobDescriptionBuilder"/> to further configure the created description.</returns>
        public static IJobDescriptionBuilder Add<T>(this IJobDescriptionCollection collection, string id, Func<T, Delegate> targetDelegate)
        {
            var targetMethod = targetDelegate.Method;

            var jobDescription = new JobDescription(null, targetMethod)
            {
                Id = id
            };

            collection.Add(jobDescription);

            return new JobDescriptionBuilder(jobDescription);
        }

        /// <summary>
        /// Adds a job description for the provided delegate and returns a builder to configure it.
        /// The job id will be generated automatically.
        /// </summary>
        /// <param name="collection">The target job description collection.</param>
        /// <param name="targetDelegate">The delegate that represents the target method (instance or static).</param>
        /// <returns>An <see cref="IJobDescriptionBuilder"/> to further configure the created description.</returns>
        public static IJobDescriptionBuilder Add(this IJobDescriptionCollection collection, Delegate targetDelegate)
        {
            var targetObject = targetDelegate.Target;
            var targetMethod = targetDelegate.Method;

            var jobDescription = new JobDescription(targetObject, targetMethod);

            collection.Add(jobDescription);

            return new JobDescriptionBuilder(jobDescription);
        }

        /// <summary>
        /// Adds a job description by resolving a method from the provided expression and returns a builder to configure it.
        /// </summary>
        /// <typeparam name="T">The type on which the method is declared.</typeparam>
        /// <param name="collection">The target job description collection.</param>
        /// <param name="targetExpression">An expression that points to a delegate factory; used to resolve the underlying <see cref="MethodInfo"/>.</param>
        /// <returns>An <see cref="IJobDescriptionBuilder"/> to further configure the created description.</returns>
        public static IJobDescriptionBuilder Add<T>(this IJobDescriptionCollection collection, Expression<Func<T, Delegate>> targetExpression)
            where T : class
        {
            var targetMethod = JobManagerMethodExpressionResolver.ResolveMethodCall(targetExpression) ?? throw new InvalidOperationException("Could not resolve method from expression.");
            var jobDescription = new JobDescription(null, targetMethod);

            collection.Add(jobDescription);

            return new JobDescriptionBuilder(jobDescription);
        }
    }
}
