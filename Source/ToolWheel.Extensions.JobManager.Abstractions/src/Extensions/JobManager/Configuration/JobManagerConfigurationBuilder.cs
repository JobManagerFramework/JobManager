using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ToolWheel.Extensions.JobManager.Conditions;
using ToolWheel.Extensions.JobManager.Middleware;

namespace ToolWheel.Extensions.JobManager.Configuration
{
    /// <summary>
    /// Fluent builder for configuring the job manager.
    /// Provides methods to configure job descriptions, register execution conditions, middleware and service factories.
    /// </summary>
    public class JobManagerConfigurationBuilder : IJobManagerConfigurationBuilder
    {
        private JobManagerConfiguration jobManagerConfiguration;
        private IServiceCollection services;

        /// <summary>
        /// Initializes a new instance of <see cref="JobManagerConfigurationBuilder"/>.
        /// </summary>
        /// <param name="jobManagerConfiguration">The configuration instance that will be populated by the builder.</param>
        /// <param name="services">The <see cref="IServiceCollection"/> used to register required services.</param>
        public JobManagerConfigurationBuilder(JobManagerConfiguration jobManagerConfiguration, IServiceCollection services)
        {
            this.jobManagerConfiguration = jobManagerConfiguration;
            this.services = services;
        }

        /// <summary>
        /// Configures the collection of job descriptions.
        /// </summary>
        /// <param name="configureJobs">An action that receives the <see cref="IJobDescriptionCollection"/> to configure.</param>
        /// <returns>The current <see cref="JobManagerConfigurationBuilder"/> instance for fluent chaining.</returns>
        public IJobManagerConfigurationBuilder ConfigureJobs(Action<IJobDescriptionCollection> configureJobs)
        {
            configureJobs(jobManagerConfiguration.Jobs);

            return this;
        }

        /// <summary>
        /// Allows configuration of additional services using the provided <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">An action that receives the <see cref="IServiceCollection"/> to configure.</param>
        /// <returns>The current <see cref="JobManagerConfigurationBuilder"/> instance for fluent chaining.</returns>
        public IJobManagerConfigurationBuilder ConfigureServices(Action<IServiceCollection> services)
        {
            services(this.services);

            return this;
        }

        /// <summary>
        /// Registers a factory for a single service type.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="serviceLifetime">The lifetime for the registered service.</param>
        /// <param name="action">A factory that produces an instance of <typeparamref name="TService"/> given an <see cref="IServiceProvider"/> and the current <see cref="IJobManagerConfiguration"/>.</param>
        /// <returns>The current <see cref="JobManagerConfigurationBuilder"/> instance for fluent chaining.</returns>
        public IJobManagerConfigurationBuilder ConfigureService<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, IJobManagerConfiguration, TService> action)
        {
            var serviceDescriptor = new ServiceDescriptor(typeof(TService), serviceProvider =>
            {
                var result = action(serviceProvider, jobManagerConfiguration);
                ArgumentNullException.ThrowIfNull(result, "The service factory returned null.");
                return result;
            }, serviceLifetime);
            services.Add(serviceDescriptor);

            return this;
        }

        /// <summary>
        /// Registers an execution condition implementation type.
        /// </summary>
        /// <typeparam name="T">The execution condition implementation type (must implement <see cref="IExecutionCondition"/>).</typeparam>
        /// <returns>The current <see cref="JobManagerConfigurationBuilder"/> instance for fluent chaining.</returns>
        public IJobManagerConfigurationBuilder AddExecutionCondition<T>()
            where T : class, IExecutionCondition
        {
            services.AddSingleton<IExecutionCondition, T>();

            return this;
        }

        /// <summary>
        /// Registers an execution condition controller implementation type.
        /// </summary>
        /// <typeparam name="T">The controller implementation type (must implement <see cref="IExecutionConditionController"/>).</typeparam>
        /// <returns>The current <see cref="JobManagerConfigurationBuilder"/> instance for fluent chaining.</returns>
        public IJobManagerConfigurationBuilder AddExecutionConditionController<T>()
            where T : class, IExecutionConditionController
        {
            services.AddSingleton<IExecutionConditionController, T>();

            return this;
        }

        /// <summary>
        /// Registers an execution middleware implementation as scoped in the DI container.
        /// </summary>
        /// <typeparam name="T">The middleware implementation type (must implement <see cref="IExecutionMiddleware"/>).</typeparam>
        /// <returns>The current <see cref="IJobManagerConfigurationBuilder"/> for fluent chaining.</returns>
        public IJobManagerConfigurationBuilder AddMiddleware<T>()
            where T : class, IExecutionMiddleware
        {
            services.AddSingleton<IExecutionMiddleware, T>();

            return this;
        }

        public IJobManagerConfigurationBuilder AddModuleDescription<T>()
             where T : class, IJobManagerModulDescription
        {
            services.AddSingleton<IJobManagerModulDescription, T>();

            return this;
        }

        /// <summary>
        /// Retrieves a feature of the specified type if present.
        /// </summary>
        /// <typeparam name="TFeature">The feature type to retrieve.</typeparam>
        /// <returns>The feature instance or <c>null</c> when not available.</returns>
        public TFeature? GetFeature<TFeature>()
            where TFeature : IJobManagerFeature
        {
            return jobManagerConfiguration.GetFeature<TFeature>();
        }

        /// <summary>
        /// Attaches or replaces a feature on this job description.
        /// </summary>
        /// <typeparam name="TFeature">The feature type to set.</typeparam>
        /// <param name="feature">The feature instance to attach.</param>
        public void SetFeature<TFeature>(TFeature feature)
            where TFeature : IJobManagerFeature
        {
            jobManagerConfiguration.SetFeature(feature);
        }
    }
}
