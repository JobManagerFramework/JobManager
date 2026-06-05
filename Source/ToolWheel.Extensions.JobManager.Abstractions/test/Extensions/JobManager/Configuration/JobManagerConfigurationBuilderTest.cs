using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Conditions;
using ToolWheel.Extensions.JobManager.Middleware;

namespace ToolWheel.Extensions.JobManager.Configuration;

[TestFixture]
public class JobManagerConfigurationBuilderTest
{
    private static void DummyTargetMethod()
    {
    }

    private sealed class TestService
    {
        public int Value { get; set; }
    }

    private sealed class SampleCondition : IExecutionCondition
    {
        public System.Threading.Tasks.Task EvaluateAsync(ExecutionConditionContext context, System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    private sealed class SampleController : IExecutionConditionController
    {
        public System.Threading.Tasks.ValueTask EvaluateAsync(ExecutionConditionContext context, System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.ValueTask.CompletedTask;
        }
    }

    private sealed class SampleMiddleware : IExecutionMiddleware
    {
        public System.Threading.Tasks.Task InvokeAsync(IJobTaskContextBuilder context, Func<System.Threading.Tasks.Task> next, System.Threading.CancellationToken cancellationToken)
        {
            return next();
        }
    }

    [Test]
    public void ConfigureJobs_InvokesAction_AddsJobDescriptionToConfiguration()
    {
        var jobManagerConfiguration = new JobManagerConfiguration();
        var services = new ServiceCollection();
        var builder = new JobManagerConfigurationBuilder(jobManagerConfiguration, services);

        var method = typeof(JobManagerConfigurationBuilderTest).GetMethod(nameof(DummyTargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var jobDescription = new JobDescription(null, method);

        var returned = builder.ConfigureJobs(jobs => jobs.Add(jobDescription));

        Assert.That(ReferenceEquals(builder, returned), Is.True);
        Assert.That(jobManagerConfiguration.Jobs.Count, Is.EqualTo(1));
        Assert.That(jobManagerConfiguration.Jobs.Contains(jobDescription), Is.True);
    }

    [Test]
    public void ConfigureServices_InvokesAction_OnServiceCollection()
    {
        var jobManagerConfiguration = new JobManagerConfiguration();
        var services = new ServiceCollection();
        var builder = new JobManagerConfigurationBuilder(jobManagerConfiguration, services);

        builder.ConfigureServices(sc => sc.AddSingleton(new TestService { Value = 123 }));

        var provider = services.BuildServiceProvider();
        var svc = provider.GetRequiredService<TestService>();

        Assert.That(svc.Value, Is.EqualTo(123));
    }

    [Test]
    public void ConfigureService_RegistersFactory_And_ResolvesInstance()
    {
        var jobManagerConfiguration = new JobManagerConfiguration();
        var services = new ServiceCollection();
        var builder = new JobManagerConfigurationBuilder(jobManagerConfiguration, services);

        builder.AddServiceFactory<TestService>(ServiceLifetime.Singleton, (sp, cfg) =>
        {
            // Use configuration to influence creation (sanity check)
            return new TestService { Value = cfg.Jobs.Count };
        });

        var provider = services.BuildServiceProvider();
        var svc = provider.GetRequiredService<TestService>();

        Assert.That(svc, Is.Not.Null);
        Assert.That(svc.Value, Is.EqualTo(0));
    }

    [Test]
    public void ConfigureService_FactoryReturnsNull_ThrowsArgumentNullExceptionOnResolve()
    {
        var jobManagerConfiguration = new JobManagerConfiguration();
        var services = new ServiceCollection();
        var builder = new JobManagerConfigurationBuilder(jobManagerConfiguration, services);

        builder.AddServiceFactory<TestService>(ServiceLifetime.Scoped, (sp, cfg) => null!);

        var provider = services.BuildServiceProvider();

        Assert.That(() => provider.GetRequiredService<TestService>(), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void AddExecutionCondition_RegistersSingleton_IExecutionCondition()
    {
        var jobManagerConfiguration = new JobManagerConfiguration();
        var services = new ServiceCollection();
        var builder = new JobManagerConfigurationBuilder(jobManagerConfiguration, services);

        builder.AddExecutionCondition<SampleCondition>();

        var provider = services.BuildServiceProvider();
        var cond = provider.GetRequiredService<IExecutionCondition>();

        Assert.That(cond, Is.InstanceOf<SampleCondition>());
    }

    [Test]
    public void AddExecutionConditionController_RegistersSingleton_IExecutionConditionController()
    {
        var jobManagerConfiguration = new JobManagerConfiguration();
        var services = new ServiceCollection();
        var builder = new JobManagerConfigurationBuilder(jobManagerConfiguration, services);

        builder.AddExecutionConditionController<SampleController>();

        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IExecutionConditionController>();

        Assert.That(controller, Is.InstanceOf<SampleController>());
    }

    [Test]
    public void AddMiddleware_RegistersScoped_IExecutionMiddleware()
    {
        var jobManagerConfiguration = new JobManagerConfiguration();
        var services = new ServiceCollection();
        var builder = new JobManagerConfigurationBuilder(jobManagerConfiguration, services);

        builder.AddMiddleware<SampleMiddleware>();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var middleware = scope.ServiceProvider.GetRequiredService<IExecutionMiddleware>();

        Assert.That(middleware, Is.InstanceOf<SampleMiddleware>());
    }
}
