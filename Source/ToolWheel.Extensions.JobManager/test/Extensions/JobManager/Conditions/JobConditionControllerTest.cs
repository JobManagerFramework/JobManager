using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Conditions;

[TestFixture]
public class JobConditionControllerTest
{
    private static JobExecutionConditionController CreateController(IEnumerable<IExecutionCondition> conditions, ILogger<JobExecutionConditionController> logger)
    {
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(s => s.GetService(typeof(IEnumerable<IExecutionCondition>))).Returns(conditions);
        return new JobExecutionConditionController(spMock.Object, logger);
    }

    private sealed class RecordingCondition : IExecutionCondition
    {
        private readonly bool _setNotReady;
        private readonly string? _message;

        public bool Called { get; private set; }

        public RecordingCondition(bool setNotReady = false, string? message = null)
        {
            _setNotReady = setNotReady;
            _message = message;
            Called = false;
        }

        public Task EvaluateAsync(ExecutionConditionContext context, CancellationToken cancellationToken)
        {
            Called = true;
            if (_setNotReady)
            {
                context.SetNotReady(_message);
            }

            return Task.CompletedTask;
        }
    }

    private static Mock<IJob> CreateJobMock(string id)
    {
        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns(id);
        jobMock.SetupGet(j => j.TargetMethod).Returns(typeof(object).GetMethod(nameof(object.ToString))!);
        jobMock.SetupGet(j => j.TargetObject).Returns((object?)null);
        jobMock.SetupGet(j => j.Name).Returns("TestJob");
        jobMock.SetupGet(j => j.Description).Returns("Test job for unit tests");
        jobMock.SetupGet(j => j.Enabled).Returns(true);
        return jobMock;
    }

    [Test]
    public async Task EvaluateAsync_NoConditions_StatusRemainsReady()
    {
        var logger = Mock.Of<ILogger<JobExecutionConditionController>>();
        var controller = CreateController(new List<IExecutionCondition>(), logger);

        var jobMock = CreateJobMock("job-1");

        var context = new ExecutionConditionContext(jobMock.Object, DateTime.UtcNow);
        await controller.EvaluateAsync(context, CancellationToken.None);
        var result = context.BuildConditionStatus();

        Assert.That(result.Status, Is.EqualTo(JobConditionStatusEnum.Ready));
    }

    [Test]
    public async Task EvaluateAsync_SingleCondition_SetsNotReady_StopsAndLogsDebug()
    {
        var loggerMock = new Mock<ILogger<JobExecutionConditionController>>();
        var condition = new RecordingCondition(setNotReady: true);
        var controller = CreateController(new[] { condition }, loggerMock.Object);

        var jobMock = CreateJobMock("job-2");

        var context = new ExecutionConditionContext(jobMock.Object, DateTime.UtcNow);
        await controller.EvaluateAsync(context, CancellationToken.None);
        var result = context.BuildConditionStatus();

        Assert.That(condition.Called, Is.True);
        Assert.That(result.Status, Is.EqualTo(JobConditionStatusEnum.NotReady));

        // Verify that a debug log was emitted when stopping evaluation
        loggerMock.Verify(l => l.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>() ),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task EvaluateAsync_MultipleConditions_StopWhenSecondSetsNotReady_ThirdNotInvoked()
    {
        var loggerMock = new Mock<ILogger<JobExecutionConditionController>>();
        var first = new RecordingCondition(); // leaves status unchanged
        var second = new RecordingCondition(setNotReady: true); // sets NotReady -> should stop after this
        var third = new RecordingCondition(); // should not be called
        var controller = CreateController(new IExecutionCondition[] { first, second, third }, loggerMock.Object);

        var jobMock = CreateJobMock("job-3");

        var context = new ExecutionConditionContext(jobMock.Object, DateTime.UtcNow);
        await controller.EvaluateAsync(context, CancellationToken.None);
        var result = context.BuildConditionStatus();

        Assert.That(first.Called, Is.True, "First condition must be called");
        Assert.That(second.Called, Is.True, "Second condition must be called");
        Assert.That(third.Called, Is.False, "Third condition must NOT be called because evaluation stops when status becomes NotReady");
        Assert.That(result.Status, Is.EqualTo(JobConditionStatusEnum.NotReady));
    }

    [Test]
    public async Task EvaluateAsync_AllConditionsReady_AllAreInvokedAndStatusRemainsReady()
    {
        var logger = Mock.Of<ILogger<JobExecutionConditionController>>();
        var a = new RecordingCondition();
        var b = new RecordingCondition();
        var c = new RecordingCondition();
        var controller = CreateController(new[] { a, b, c }, logger);

        var jobMock = CreateJobMock("job-4");

        var context = new ExecutionConditionContext(jobMock.Object, DateTime.UtcNow);
        await controller.EvaluateAsync(context, CancellationToken.None);
        var result = context.BuildConditionStatus();

        Assert.That(a.Called, Is.True);
        Assert.That(b.Called, Is.True);
        Assert.That(c.Called, Is.True);
        Assert.That(result.Status, Is.EqualTo(JobConditionStatusEnum.Ready));
    }

    [Test]
    public async Task EvaluateAsync_NotReadyCondition_MessageIsForwarded()
    {
        var logger = Mock.Of<ILogger<JobExecutionConditionController>>();
        var condition = new RecordingCondition(setNotReady: true, message: "Custom reason");
        var controller = CreateController(new[] { condition }, logger);

        var jobMock = CreateJobMock("job-5");

        var context = new ExecutionConditionContext(jobMock.Object, DateTime.UtcNow);
        await controller.EvaluateAsync(context, CancellationToken.None);
        var result = context.BuildConditionStatus();

        Assert.That(result.Status, Is.EqualTo(JobConditionStatusEnum.NotReady));
        Assert.That(result.Message, Is.EqualTo("Custom reason"));
    }
}
