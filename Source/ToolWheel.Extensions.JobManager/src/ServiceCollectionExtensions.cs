using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ToolWheel.Extensions.JobManager.Conditions;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Middleware;
using ToolWheel.Extensions.JobManager.Services;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel
{
    /// <summary>
    /// Provides extension methods to register the JobManager and its supporting services into an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the job manager infrastructure and optional configuration callback into the provided <paramref name="services"/> collection.
        /// This method registers default implementations for event publishing, task management, middleware and condition controllers,
        /// and allows the caller to further configure jobs, middleware and DI services via the <paramref name="configure"/> callback.
        /// </summary>
        /// <param name="services">The service collection to register job manager services into.</param>
        /// <param name="configure">
        /// Optional configuration action that receives an <see cref="IJobManagerConfigurationBuilder"/> to configure jobs, conditions, middleware and services.
        /// </param>
        /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
        public static IServiceCollection AddJobManager(this IServiceCollection services, Action<IJobManagerConfigurationBuilder>? configure = null)
        {
            var jobManagerConfiguration = new JobManagerConfiguration();
            var jobManagerConfigurationBuilder = new JobManagerConfigurationBuilder(jobManagerConfiguration, services);

            // Logging
            services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.TryAddTransient(typeof(ILogger<>), typeof(Logger<>));

            // Storages
            services.AddSingleton<IExtensionOptionStorage, InMemoryExtensionOptionStorage>();
            services.AddSingleton<IJobOptionStorage, InMemoryJobOptionStorage>();
            services.AddSingleton<IJobStorage, InMemoryJobStorage>();
            services.AddSingleton<IJobTaskJournalStorage, InMemoryJobTaskJournalStorage>();
            services.AddSingleton<IJobTaskStorage, InMemoryJobTaskStorage>();

            // Services
            services.AddSingleton<IJobExecutionConditionService, JobExecutionConditionService>();
            services.AddSingleton<IJobExecutionEvaluatorService, JobExecutionEvaluatorService>();
            services.AddSingleton<IJobExecutionStatisticsService, JobExecutionStatisticsService>();
            services.AddSingleton<IJobOptionService, JobOptionService>();
            services.AddSingleton<IExtensionOptionService, ExtensionOptionService>();
            services.AddSingleton<IJobTaskExecutionService, JobTaskExecutionService>();
            services.AddSingleton<IJobTaskJournalService, JobTaskJournalService>();
            services.AddSingleton<IJobTaskService, JobTaskService>();

            // Factory / JobService
            jobManagerConfigurationBuilder.AddServiceFactory(ServiceLifetime.Singleton, CreateJobService);

            // Conditions & Controllers
            jobManagerConfigurationBuilder.AddExecutionConditionController<JobExecutionConditionController>();

            // Middleware
            jobManagerConfigurationBuilder.AddMiddleware<JobTaskTargetObjectMiddleware>();
            jobManagerConfigurationBuilder.AddMiddleware<JobExecutionStatisticsMiddleware>();

            configure?.Invoke(jobManagerConfigurationBuilder);

            AutoInstallJobManagerModules(jobManagerConfigurationBuilder);

            return services;
        }

        /// <summary>
        /// Registers the job manager infrastructure using a custom <typeparamref name="TStartup"/> class for configuration.
        /// This overload allows you to encapsulate job manager and job registration logic in a dedicated startup class.
        /// </summary>
        /// <typeparam name="TStartup">
        /// The startup class that derives from <see cref="JobManagerStartup"/> and provides configuration logic.
        /// </typeparam>
        /// <param name="services">The service collection to register job manager services into.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
        public static IServiceCollection AddJobManager<TStartup>(this IServiceCollection services)
            where TStartup : JobManagerStartup, new()
        {
            var startup = new TStartup();

            return services.AddJobManager(configure =>
            {
                startup.ConfigureJobManager(configure);

                configure.ConfigureJobs(startup.ConfigureJobs);
            });

        }

        private static void AutoInstallJobManagerModules(IJobManagerConfigurationBuilder builder)
        {
            foreach (var assembly in AssemblyDiscovery.GetCandidateAssemblies())
            {
                var types = from type in assembly.GetTypes()
                            where type is not null && type.IsClass && !type.IsAbstract && typeof(IJobManagerModulDescription).IsAssignableFrom(type)
                            select type;

                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is IJobManagerModulDescription configurator)
                    {
                        try
                        {
                            configurator.ModuleConfiguration(builder);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.TraceWarning(
                                "JobManager Module failed for type '{0}': {1}", type.FullName, ex.Message);
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Factory method that creates the <see cref="IJobService"/> and registers all configured job descriptions.
        /// </summary>
        /// <param name="serviceProvider">The application service provider.</param>
        /// <param name="configuration">The job manager configuration containing the job descriptions to register.</param>
        /// <returns>The fully initialised <see cref="IJobService"/> instance.</returns>
        private static IJobService CreateJobService(IServiceProvider serviceProvider, IJobManagerConfiguration configuration)
        {
            var service = ActivatorUtilities.CreateInstance<JobService>(serviceProvider);

            foreach (var job in configuration.Jobs)
            {
                service.Add(job);
            }

            return service;
        }
    }
}

