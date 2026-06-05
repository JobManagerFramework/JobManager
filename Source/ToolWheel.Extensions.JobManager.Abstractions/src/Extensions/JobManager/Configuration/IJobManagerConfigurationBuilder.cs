using System;
using Microsoft.Extensions.DependencyInjection;
using ToolWheel.Extensions.JobManager.Conditions;
using ToolWheel.Extensions.JobManager.Middleware;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Fluent builder for configuring the job manager, its jobs, execution conditions, middleware and services.
    /// </summary>
    public interface IJobManagerConfigurationBuilder
    {
        /// <summary>
        /// Configures the collection of job descriptions.
        /// </summary>
        /// <param name="configureJobs">An action that receives the job description collection to configure.</param>
        IJobManagerConfigurationBuilder ConfigureJobs(Action<IJobDescriptionCollection> configureJobs);

        /// <summary>
        /// Registers an execution condition type that will be used to evaluate whether jobs may run.
        /// </summary>
        /// <typeparam name="T">The execution condition implementation type.</typeparam>
        IJobManagerConfigurationBuilder AddExecutionCondition<T>() where T : class, IExecutionCondition;

        /// <summary>
        /// Registers a controller that coordinates execution condition evaluation.
        /// </summary>
        /// <typeparam name="T">The execution condition controller implementation type.</typeparam>
        IJobManagerConfigurationBuilder AddExecutionConditionController<T>() where T : class, IExecutionConditionController;

        /// <summary>
        /// Adds an execution middleware type to the pipeline.
        /// </summary>
        /// <typeparam name="T">The middleware implementation type.</typeparam>
        IJobManagerConfigurationBuilder AddMiddleware<T>() where T : class, IExecutionMiddleware;

        /// <summary>
        /// Registers a module description which can be used to group related job configurations and features together.
        /// </summary>
        /// <typeparam name="T">The module description type (must implement <see cref="IJobManagerModulDescription"/>).</typeparam>
        /// <returns></returns>
        [Obsolete("Modules are deprecated in favor of direct configuration. Please configure your services and features directly on the builder.", error: false)]
        IJobManagerConfigurationBuilder AddModuleDescription<T>() where T : class, IJobManagerModulDescription;

        /// <summary>
        /// Configures additional services for the job manager by providing an <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">An action that receives the service collection to configure.</param>
        IJobManagerConfigurationBuilder ConfigureServices(Action<IServiceCollection> services);

        /// <summary>
        /// Configures a single service factory for the job manager configuration.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="serviceLifetime">The lifetime for the registered service.</param>
        /// <param name="action">A factory that creates the service given an <see cref="IServiceProvider"/> and the current <see cref="IJobManagerConfiguration"/>.</param>
        IJobManagerConfigurationBuilder AddServiceFactory<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, IJobManagerConfiguration, TService> action);

        /// <summary>
        /// Retrieves the specified feature associated with the job manager.
        /// </summary>
        /// <typeparam name="TFeature">The type of feature to retrieve, which must implement IJobManagerFeature.</typeparam>
        /// <returns>The feature instance if found; otherwise, null.</returns>
        TFeature? GetFeature<TFeature>()
            where TFeature : IJobManagerFeature;

        /// <summary>
        /// Associates a feature with the job manager.
        /// </summary>
        /// <typeparam name="TFeature">The type of feature to set, implementing IJobManagerFeature.</typeparam>
        /// <param name="feature">The feature instance to associate.</param>
        void SetFeature<TFeature>(TFeature feature)
            where TFeature : IJobManagerFeature;
    }
}
