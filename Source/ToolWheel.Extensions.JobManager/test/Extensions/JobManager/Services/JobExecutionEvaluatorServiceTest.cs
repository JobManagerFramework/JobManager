using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Conditions;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class JobExecutionEvaluatorServiceTest
{
    private static JobExecutionEvaluatorService CreateService(
        IEnumerable<IExecutionConditionController> controllers,
        ILogger<JobExecutionEvaluatorService> logger)
    {
        var spMock = new Mock<IServiceProvider>();
        spMock
            .Setup(s => s.GetService(typeof(IEnumerable<IExecutionConditionController>)))
            .Returns(controllers);
        return new JobExecutionEvaluatorService(spMock.Object, logger);
    }

    private Mock<IJob> CreateJobMock()
    {
        var method = typeof(object).GetMethod(nameof(object.ToString))!;
        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns(Guid.NewGuid().ToString());
        jobMock.SetupGet(j => j.TargetMethod).Returns(method);
        jobMock.SetupGet(j => j.TargetObject).Returns((object?)null);
        jobMock.SetupGet(j => j.Name).Returns("test");
        jobMock.SetupGet(j => j.Description).Returns("desc");
        jobMock.SetupGet(j => j.Enabled).Returns(true);
        return jobMock;
    }

    private static Mock<IExecutionConditionController> CreateControllerMock(bool setNotReady = false, string? message = null)
    {
        var mock = new Mock<IExecutionConditionController>();
        mock
            .Setup(c => c.EvaluateAsync(It.IsAny<ExecutionConditionContext>(), It.IsAny<CancellationToken>()))
            .Callback<ExecutionConditionContext, CancellationToken>((builder, ct) =>
            {
                if (setNotReady)
                {
                    builder.SetNotReady(message);
                }
            })
            .Returns(ValueTask.CompletedTask);
        return mock;
    }

    [Test]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var loggerMock = new Mock<ILogger<JobExecutionEvaluatorService>>();
        Assert.That(() => new JobExecutionEvaluatorService(null!, loggerMock.Object), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var spMock = new Mock<IServiceProvider>();
        Assert.That(() => new JobExecutionEvaluatorService(spMock.Object, null!), Throws.ArgumentNullException);
    }

    [Test]
    public async Task EvaluateAsync_ReturnsReady_WhenNoControllersRegistered()
    {
        // Arrange
        var controllers = Array.Empty<IExecutionConditionController>();
        var loggerMock = new Mock<ILogger<JobExecutionEvaluatorService>>();
        var svc = CreateService(controllers, loggerMock.Object);
        var jobMock = CreateJobMock();

        // Act
        var status = await svc.EvaluateAsync(jobMock.Object).ConfigureAwait(false);

        // Assert
        Assert.That(status.Status, Is.EqualTo(JobConditionStatusEnum.Ready));
    }

    [Test]
    public async Task EvaluateAsync_ReturnsReady_WhenAllControllersLeaveReady()
    {
        // Arrange
        var ctl1 = CreateControllerMock();
        var ctl2 = CreateControllerMock();

        var loggerMock = new Mock<ILogger<JobExecutionEvaluatorService>>();
        var svc = CreateService(new[] { ctl1.Object, ctl2.Object }, loggerMock.Object);
        var jobMock = CreateJobMock();

        // Act
        var status = await svc.EvaluateAsync(jobMock.Object).ConfigureAwait(false);

        // Assert
        Assert.That(status.Status, Is.EqualTo(JobConditionStatusEnum.Ready));
        ctl1.Verify(c => c.EvaluateAsync(It.IsAny<ExecutionConditionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        ctl2.Verify(c => c.EvaluateAsync(It.IsAny<ExecutionConditionContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task EvaluateAsync_StopsAndReturnsNotReady_WhenControllerSetsNotReady()
    {
        // Arrange
        var ctl1 = CreateControllerMock();
        var ctl2 = CreateControllerMock(setNotReady: true);

        // third controller should NOT be called because evaluation stops after ctl2
        var ctl3 = CreateControllerMock();

        var loggerMock = new Mock<ILogger<JobExecutionEvaluatorService>>();
        var svc = CreateService(new[] { ctl1.Object, ctl2.Object, ctl3.Object }, loggerMock.Object);
        var jobMock = CreateJobMock();

        // Act
        var status = await svc.EvaluateAsync(jobMock.Object).ConfigureAwait(false);

        // Assert
        Assert.That(status.Status, Is.EqualTo(JobConditionStatusEnum.NotReady));
        ctl3.Verify(c => c.EvaluateAsync(It.IsAny<ExecutionConditionContext>(), It.IsAny<CancellationToken>()), Times.Never,
            "Third controller must NOT be called after short-circuit");
    }

    [Test]
    public async Task EvaluateAsync_ContinuesOnControllerException_AndReturnsReadyWhenNoNotReady()
    {
        // Arrange
        var ctl1 = new Mock<IExecutionConditionController>();
        ctl1
            .Setup(c => c.EvaluateAsync(It.IsAny<ExecutionConditionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var ctl2 = CreateControllerMock();

        var loggerMock = new Mock<ILogger<JobExecutionEvaluatorService>>();
        var svc = CreateService(new[] { ctl1.Object, ctl2.Object }, loggerMock.Object);
        var jobMock = CreateJobMock();

        // Act
        var status = await svc.EvaluateAsync(jobMock.Object).ConfigureAwait(false);

        // Assert – Exception im ersten Controller wird geloggt/ignoriert, Auswertung geht weiter
        Assert.That(status.Status, Is.EqualTo(JobConditionStatusEnum.Ready));
    }

    [Test]
    public void EvaluateAsync_PropagatesOperationCanceledException()
    {
        // Arrange
        var ctl = new Mock<IExecutionConditionController>();
        ctl
            .Setup(c => c.EvaluateAsync(It.IsAny<ExecutionConditionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var loggerMock = new Mock<ILogger<JobExecutionEvaluatorService>>();
        var svc = CreateService(new[] { ctl.Object }, loggerMock.Object);
        var jobMock = CreateJobMock();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () => await svc.EvaluateAsync(jobMock.Object).ConfigureAwait(false));
    }

    [Test]
    public void EvaluateAsync_ThrowsWhenCancellationAlreadyRequested()
    {
        // Arrange
        var ctl = CreateControllerMock();

        var loggerMock = new Mock<ILogger<JobExecutionEvaluatorService>>();
        var svc = CreateService(new[] { ctl.Object }, loggerMock.Object);
        var jobMock = CreateJobMock();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert – bereits abgebrochenes Token muss OperationCanceledException auslösen
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await svc.EvaluateAsync(jobMock.Object, cts.Token).ConfigureAwait(false));
    }
}
