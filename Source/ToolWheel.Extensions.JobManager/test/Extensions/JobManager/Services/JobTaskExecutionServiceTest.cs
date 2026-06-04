using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Middleware;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class JobTaskExecutionServiceTest
{
    // Hilfsklasse mit statischen Target-Methoden, die per Reflection aufgerufen werden.
    public static class TestTargets
    {
        public static int Return42() => 42;

        public static Task<int> ReturnTask42() => Task.FromResult(42);

        public static void ThrowOperationCanceled() => throw new OperationCanceledException();

        public static void ThrowInvalidOperation() => throw new InvalidOperationException("boom");
    }

    private static JobTaskExecutionService CreateSut(
        Mock<IServiceScopeFactory> factory,
        Mock<ILogger<JobTaskExecutionService>> logger,
        Mock<IJobExecutionEvaluatorService> evaluator,
        IJobTaskJournalService? journalService = null)
    {
        return new JobTaskExecutionService(
            factory.Object,
            logger.Object,
            evaluator.Object,
            journalService ?? new Mock<IJobTaskJournalService>().Object);
    }

    private Mock<IServiceScopeFactory> CreateDefaultServiceScopeFactory()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        // Ensure that requesting middlewares returns an empty array instead of null,
        // so JobTaskExecutionService can enumerate middlewares safely.
        serviceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IExecutionMiddleware>)))
            .Returns(Array.Empty<IExecutionMiddleware>());

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);

        return factory;
    }

    private Mock<IJobExecutionEvaluatorService> CreateReadyEvaluator()
    {
        var evaluator = new Mock<IJobExecutionEvaluatorService>();
        evaluator
            .Setup(e => e.EvaluateAsync(It.IsAny<IJob>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<JobConditionStatus>(JobConditionStatus.Ready));
        return evaluator;
    }

    // Erzeugt einen Mock für IJob - alle benötigten Schnittstellen werden gemockt.
    private Mock<IJob> CreateJobMock(MethodInfo method)
    {
        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.TargetMethod).Returns(method);
        jobMock.SetupGet(j => j.TargetObject).Returns((object?)null);
        jobMock.SetupGet(j => j.Id).Returns(Guid.NewGuid().ToString());
        jobMock.SetupGet(j => j.Name).Returns("test");
        jobMock.SetupGet(j => j.Description).Returns("test");
        jobMock.SetupGet(j => j.Enabled).Returns(true);
        jobMock.SetupGet(j => j.JobLogger).Returns((ILogger?)null);
        return jobMock;
    }

    [Test]
    public async Task Execute_WhenMiddlewareSetsConditionNotReady_MiddlewareIsCalled_AndJobIsAborted()
    {
        // Arrange
        var middlewareMock = new Mock<IExecutionMiddleware>();
        JobConditionStatus? capturedStatus = null;
        middlewareMock
            .Setup(m => m.InvokeAsync(It.IsAny<IJobTaskContextBuilder>(), It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns((IJobTaskContextBuilder ctx, Func<Task> next, CancellationToken ct) =>
            {
                // Middleware marks condition as NotReady and still calls next().
                ctx.ConditionStatus = JobConditionStatus.NotReady;
                capturedStatus = ctx.ConditionStatus;
                return next();
            });

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IExecutionMiddleware>)))
            .Returns(new[] { middlewareMock.Object });

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var factoryMock = new Mock<IServiceScopeFactory>();
        factoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        var evaluatorMock = CreateReadyEvaluator();

        var service = CreateSut(factoryMock, loggerMock, evaluatorMock);

        var jobMock = CreateJobMock(typeof(TestTargets).GetMethod(nameof(TestTargets.Return42))!);

        // Act
        var jobTask = await service.ExecuteAsync(jobMock.Object);

        Assert.That(jobTask.Task, Is.Not.Null, "Task should have been created.");

        await jobTask.Task!.ConfigureAwait(false);

        // Assert
        // Middleware was executed and set the condition to NotReady
        Assert.That(capturedStatus, Is.Not.Null);
        Assert.That(capturedStatus!.Status, Is.EqualTo(JobConditionStatusEnum.NotReady));

        // Execution should have been aborted due to NotReady condition set by middleware during pipeline
        Assert.That(jobTask.Status, Is.EqualTo(JobTaskStatusEnum.NotReady));
        Assert.That(jobTask.Result, Is.Null);
    }

    [Test]
    public async Task Execute_WhenReady_InvokesStaticMethodAndSetsResultAndStatusSuccess()
    {
        // Arrange
        var factory = CreateDefaultServiceScopeFactory();
        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        var evaluatorMock = CreateReadyEvaluator();

        var service = CreateSut(factory, loggerMock, evaluatorMock);

        var jobMock = CreateJobMock(typeof(TestTargets).GetMethod(nameof(TestTargets.Return42))!);

        // Act
        var jobTask = await service.ExecuteAsync(jobMock.Object);

        Assert.That(jobTask.Task, Is.Not.Null, "Task should have been created for a ready job.");

        await jobTask.Task!.ConfigureAwait(false);

        // Assert jobTask state
        Assert.That(jobTask.Status, Is.EqualTo(JobTaskStatusEnum.Success));
        Assert.That(jobTask.Result, Is.EqualTo(42));
    }

    [Test]
    public async Task ExecuteAsync_WhenTargetReturnsTask_InvokesAndSetsResultAndStatusSuccess()
    {
        // Arrange
        var factory = CreateDefaultServiceScopeFactory();
        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        var evaluatorMock = CreateReadyEvaluator();

        var service = CreateSut(factory, loggerMock, evaluatorMock);

        var jobMock = CreateJobMock(typeof(TestTargets).GetMethod(nameof(TestTargets.ReturnTask42))!);

        // Act
        var jobTask = await service.ExecuteAsync(jobMock.Object);

        Assert.That(jobTask.Task, Is.Not.Null, "Task should have been created for a ready job.");

        await jobTask.Task!.ConfigureAwait(false);

        // Assert jobTask state for async return
        Assert.That(jobTask.Status, Is.EqualTo(JobTaskStatusEnum.Success));
        Assert.That(jobTask.Result, Is.EqualTo(42));
    }

    [Test]
    public async Task ExecuteAsync_WhenTargetThrowsOperationCanceled_SetsCanceledStatus()
    {
        // Arrange
        var factory = CreateDefaultServiceScopeFactory();
        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        var evaluatorMock = CreateReadyEvaluator();

        var service = CreateSut(factory, loggerMock, evaluatorMock);

        var jobMock = CreateJobMock(typeof(TestTargets).GetMethod(nameof(TestTargets.ThrowOperationCanceled))!);

        // Act
        var jobTask = await service.ExecuteAsync(jobMock.Object);

        Assert.That(jobTask.Task, Is.Not.Null, "Task should have been created even when target cancels.");

        await jobTask.Task!.ConfigureAwait(false);

        // Assert
        Assert.That(jobTask.Status, Is.EqualTo(JobTaskStatusEnum.Canceled));
    }

    [Test]
    public async Task ExecuteAsync_WhenTargetThrowsException_SetsFailedAndStoresException()
    {
        // Arrange
        var factory = CreateDefaultServiceScopeFactory();
        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        var evaluatorMock = CreateReadyEvaluator();

        var service = CreateSut(factory, loggerMock, evaluatorMock);

        var jobMock = CreateJobMock(typeof(TestTargets).GetMethod(nameof(TestTargets.ThrowInvalidOperation))!);

        // Act
        var jobTask = await service.ExecuteAsync(jobMock.Object);

        Assert.That(jobTask.Task, Is.Not.Null);

        await jobTask.Task!.ConfigureAwait(false);

        // Assert
        Assert.That(jobTask.Status, Is.EqualTo(JobTaskStatusEnum.Failed));
        Assert.That(jobTask.Result, Is.InstanceOf<InvalidOperationException>());
        Assert.That(((InvalidOperationException)jobTask.Result!).Message, Is.EqualTo("boom"));
    }

    [Test]
    public async Task ExecuteAsync_WhenMiddlewareThrowsOperationCanceled_SetsCanceledStatus()
    {
        // Arrange
        // Create a scope factory that resolves IEnumerable<IExecutionMiddleware> containing a middleware that throws.
        var middlewareMock = new Mock<IExecutionMiddleware>();
        middlewareMock
            .Setup(m => m.InvokeAsync(It.IsAny<IJobTaskContextBuilder>(), It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IExecutionMiddleware>)))
            .Returns(new[] { middlewareMock.Object });

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var factoryMock = new Mock<IServiceScopeFactory>();
        factoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        var evaluatorMock = CreateReadyEvaluator();

        var service = CreateSut(factoryMock, loggerMock, evaluatorMock);

        var jobMock = CreateJobMock(typeof(TestTargets).GetMethod(nameof(TestTargets.Return42))!);

        // Act
        var jobTask = await service.ExecuteAsync(jobMock.Object);

        Assert.That(jobTask.Task, Is.Not.Null, "Task should have been created even when middleware cancels.");

        await jobTask.Task!.ConfigureAwait(false);

        // Assert
        Assert.That(jobTask.Status, Is.EqualTo(JobTaskStatusEnum.Canceled));
    }

    [Test]
    public async Task ExecuteAsync_WhenMiddlewareThrowsException_SetsFailedAndStoresException()
    {
        // Arrange
        var middlewareMock = new Mock<IExecutionMiddleware>();
        var ex = new InvalidOperationException("middleware fail");
        middlewareMock
            .Setup(m => m.InvokeAsync(It.IsAny<IJobTaskContextBuilder>(), It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IExecutionMiddleware>)))
            .Returns(new[] { middlewareMock.Object });

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var factoryMock = new Mock<IServiceScopeFactory>();
        factoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        var evaluatorMock = CreateReadyEvaluator();

        var service = CreateSut(factoryMock, loggerMock, evaluatorMock);

        var jobMock = CreateJobMock(typeof(TestTargets).GetMethod(nameof(TestTargets.Return42))!);

        // Act
        var jobTask = await service.ExecuteAsync(jobMock.Object);

        Assert.That(jobTask.Task, Is.Not.Null);

        await jobTask.Task!.ConfigureAwait(false);

        // Assert
        Assert.That(jobTask.Status, Is.EqualTo(JobTaskStatusEnum.Failed));
        Assert.That(jobTask.Result, Is.SameAs(ex));
    }

    [Test]
    public void Constructor_NullServiceScopeFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        var evaluatorMock = CreateReadyEvaluator();

        // Act & Assert
        Assert.That(() => new JobTaskExecutionService(null!, loggerMock.Object, evaluatorMock.Object, new Mock<IJobTaskJournalService>().Object), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = CreateDefaultServiceScopeFactory();
        var evaluatorMock = CreateReadyEvaluator();

        // Act & Assert
        Assert.That(() => new JobTaskExecutionService(factory.Object, null!, evaluatorMock.Object, new Mock<IJobTaskJournalService>().Object), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullExecutionEvaluator_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = CreateDefaultServiceScopeFactory();
        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();

        // Act & Assert
        Assert.That(() => new JobTaskExecutionService(factory.Object, loggerMock.Object, null!, new Mock<IJobTaskJournalService>().Object), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullJournalService_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = CreateDefaultServiceScopeFactory();
        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        var evaluatorMock = CreateReadyEvaluator();

        // Act & Assert
        Assert.That(() => new JobTaskExecutionService(factory.Object, loggerMock.Object, evaluatorMock.Object, null!), Throws.ArgumentNullException);
    }

    [Test]
    public async Task ExecuteAsync_WhenJobLoggerIsNull_JournalForwardsToApplicationLogger()
    {
        // Arrange
        var factory = CreateDefaultServiceScopeFactory();
        var loggerMock = new Mock<ILogger<JobTaskExecutionService>>();
        loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var evaluatorMock = CreateReadyEvaluator();

        var service = CreateSut(factory, loggerMock, evaluatorMock);

        // JobLogger is null → application logger should act as fallback
        var jobMock = CreateJobMock(typeof(TestTargets).GetMethod(nameof(TestTargets.Return42))!);
        jobMock.SetupGet(j => j.JobLogger).Returns((ILogger?)null);

        // Act
        var jobTask = await service.ExecuteAsync(jobMock.Object);
        Assert.That(jobTask.Task, Is.Not.Null);
        await jobTask.Task!.ConfigureAwait(false);

        // Assert: at least one journal-originated log call reached the application logger
        loggerMock.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
