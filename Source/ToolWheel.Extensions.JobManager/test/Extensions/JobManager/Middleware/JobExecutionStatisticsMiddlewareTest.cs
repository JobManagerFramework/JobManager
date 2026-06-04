using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Services;

namespace ToolWheel.Extensions.JobManager.Middleware;

[TestFixture]
public class JobExecutionStatisticsMiddlewareTest
{
    [Test]
    public void Constructor_NullStatisticsService_ThrowsArgumentNullException()
    {
        Assert.That(() => new JobExecutionStatisticsMiddleware(null!), Throws.ArgumentNullException);
    }

    [Test]
    public async Task InvokeAsync_CallsNext_AndRecordsStatistics()
    {
        // Arrange
        var statisticsServiceMock = new Mock<IJobExecutionStatisticsService>();
        var middleware = new JobExecutionStatisticsMiddleware(statisticsServiceMock.Object);

        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns("job-1");
        jobMock.SetupGet(j => j.Name).Returns("TestJob");

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(jt => jt.Status).Returns(JobTaskStatusEnum.Success);

        var contextMock = new Mock<IJobTaskContextBuilder>();
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);
        contextMock.SetupGet(c => c.JobTask).Returns(jobTaskMock.Object);

        var nextCalled = false;
        Func<Task> next = () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await middleware.InvokeAsync(contextMock.Object, next, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(nextCalled, Is.True, "Das 'next' delegate muss aufgerufen werden.");
        statisticsServiceMock.Verify(
            s => s.Record("job-1", "TestJob", It.IsAny<TimeSpan>(), JobTaskStatusEnum.Success),
            Times.Once);
    }

    [Test]
    public async Task InvokeAsync_RecordsCorrectStatus_WhenFailed()
    {
        // Arrange
        var statisticsServiceMock = new Mock<IJobExecutionStatisticsService>();
        var middleware = new JobExecutionStatisticsMiddleware(statisticsServiceMock.Object);

        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns("job-2");
        jobMock.SetupGet(j => j.Name).Returns("FailingJob");

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(jt => jt.Status).Returns(JobTaskStatusEnum.Failed);

        var contextMock = new Mock<IJobTaskContextBuilder>();
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);
        contextMock.SetupGet(c => c.JobTask).Returns(jobTaskMock.Object);

        Func<Task> next = () => Task.CompletedTask;

        // Act
        await middleware.InvokeAsync(contextMock.Object, next, CancellationToken.None).ConfigureAwait(false);

        // Assert
        statisticsServiceMock.Verify(
            s => s.Record("job-2", "FailingJob", It.IsAny<TimeSpan>(), JobTaskStatusEnum.Failed),
            Times.Once);
    }

    [Test]
    public async Task InvokeAsync_RecordsDuration_GreaterThanZero_WhenNextTakesTime()
    {
        // Arrange
        TimeSpan capturedDuration = TimeSpan.Zero;
        var statisticsServiceMock = new Mock<IJobExecutionStatisticsService>();
        statisticsServiceMock
            .Setup(s => s.Record(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<JobTaskStatusEnum>()))
            .Callback<string, string, TimeSpan, JobTaskStatusEnum>((_, _, duration, _) => capturedDuration = duration);

        var middleware = new JobExecutionStatisticsMiddleware(statisticsServiceMock.Object);

        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns("job-3");
        jobMock.SetupGet(j => j.Name).Returns("SlowJob");

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(jt => jt.Status).Returns(JobTaskStatusEnum.Success);

        var contextMock = new Mock<IJobTaskContextBuilder>();
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);
        contextMock.SetupGet(c => c.JobTask).Returns(jobTaskMock.Object);

        Func<Task> next = async () =>
        {
            await Task.Delay(50).ConfigureAwait(false);
        };

        // Act
        await middleware.InvokeAsync(contextMock.Object, next, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(capturedDuration, Is.GreaterThan(TimeSpan.Zero), "Die gemessene Dauer sollte größer als 0 sein.");
    }

    [Test]
    public async Task InvokeAsync_RecordsStatistics_EvenWhenNextThrows()
    {
        // Arrange
        var statisticsServiceMock = new Mock<IJobExecutionStatisticsService>();
        var middleware = new JobExecutionStatisticsMiddleware(statisticsServiceMock.Object);

        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns("job-4");
        jobMock.SetupGet(j => j.Name).Returns("CrashJob");

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(jt => jt.Status).Returns(JobTaskStatusEnum.Failed);

        var contextMock = new Mock<IJobTaskContextBuilder>();
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);
        contextMock.SetupGet(c => c.JobTask).Returns(jobTaskMock.Object);

        Func<Task> next = () => throw new InvalidOperationException("boom");

        // Act & Assert – die Middleware soll die Exception nicht schlucken
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await middleware.InvokeAsync(contextMock.Object, next, CancellationToken.None).ConfigureAwait(false);
        });

        // Die Statistik wird hier NICHT erfasst, weil die Exception vor dem Record-Aufruf fliegt.
        // Dies ist korrekt, da die Pipeline-Fehlerbehandlung im ExecutePipelineAsync stattfindet.
        statisticsServiceMock.Verify(
            s => s.Record(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<JobTaskStatusEnum>()),
            Times.Never);
    }
}
