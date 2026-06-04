using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class JobTaskServiceTest
{
    private sealed class TestTarget
    {
        public void DoWork() { }
    }

    private static JobTaskService CreateSut(Mock<ILogger<JobTaskService>> loggerMock, Mock<IJobTaskExecutionService> execMock)
    {
        var journalMock = new Mock<IJobTaskJournalService>();
        return new JobTaskService(loggerMock.Object, execMock.Object, new InMemoryJobTaskStorage(), journalMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenCalled_DelegatesToExecutionServiceAndStoresTask()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job1", null, method,false);

        var jobTaskMock = new Mock<IJobTask>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        jobTaskMock.SetupGet(t => t.Job).Returns(job);
        jobTaskMock.SetupGet(t => t.Id).Returns("task1");

        var execMock = new Mock<IJobTaskExecutionService>();
        execMock.Setup(s => s.ExecuteAsync(It.Is<IJob>(j => j.Id == job.Id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobTaskMock.Object)
                .Verifiable();

        var sut = CreateSut(loggerMock, execMock);

        // Act
        var result = await sut.ExecuteAsync(job);

        // Assert
        Assert.That(result, Is.SameAs(jobTaskMock.Object));
        Assert.That(sut.ReadByJob(job).Single(), Is.SameAs(jobTaskMock.Object));
        Assert.That(sut.ReadAll().Contains(jobTaskMock.Object), Is.True);
        execMock.Verify();
    }

    [Test]
    public void ReadAll_InitiallyEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        // Act & Assert
        Assert.That(sut.ReadAll(), Is.Empty);
    }

    [Test]
    public void ReadByJob_IfJobNotPresent_ReturnsEmptyCollection()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("missing", null, method, false);
        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        // Act & Assert: previously threw KeyNotFoundException
        Assert.That(sut.ReadByJob(job), Is.Empty);
    }

    [Test]
    public async Task Remove_ExistingTask_RemovesFromJobList()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job2", null, method, false);

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(t => t.Job).Returns(job);
        jobTaskMock.SetupGet(t => t.Id).Returns("task2");

        var execMock = new Mock<IJobTaskExecutionService>();
        execMock.Setup(s => s.ExecuteAsync(It.IsAny<IJob>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobTaskMock.Object);

        var loggerMock = new Mock<ILogger<JobTaskService>>();

        var sut = CreateSut(loggerMock, execMock);

        // add task
        var added = await sut.ExecuteAsync(job);
        Assert.That(sut.ReadByJob(job), Is.Not.Empty);

        // Act
        sut.Remove(added);

        // Assert
        Assert.That(sut.ReadByJob(job), Is.Empty);
    }

    [Test]
    public void Remove_NonExistingTask_DoesNotThrow_LeavesEmptyList()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job3", null, method, false);

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(t => t.Job).Returns(job);
        jobTaskMock.SetupGet(t => t.Id).Returns("task3");

        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();

        var sut = CreateSut(loggerMock, execMock);

        // Act (should not throw)
        Assert.That(() => sut.Remove(jobTaskMock.Object), Throws.Nothing);

        // After removal a list for the job exists but is empty
        Assert.That(sut.ReadByJob(job), Is.Empty);
    }

    [Test]
    public void Remove_NullTask_ThrowsArgumentNullException()
    {
        // Arrange
        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        // Act & Assert
        Assert.That(() => sut.Remove(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void ReadByJob_NullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        // Act & Assert
        Assert.That(() => sut.ReadByJob(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void ReadByJobWithStatus_NullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        // Act & Assert
        Assert.That(() => sut.ReadByJob(null!, JobTaskStatusEnum.Running), Throws.ArgumentNullException);
    }

    [Test]
    public async Task ReadByJob_WithMatchingStatus_ReturnsOnlyMatchingTasks()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job-status", null, method, false);

        var runningTaskMock = new Mock<IJobTask>();
        runningTaskMock.SetupGet(t => t.Job).Returns(job);
        runningTaskMock.SetupGet(t => t.Id).Returns("task-running");
        runningTaskMock.SetupGet(t => t.Status).Returns(JobTaskStatusEnum.Running);

        var successTaskMock = new Mock<IJobTask>();
        successTaskMock.SetupGet(t => t.Job).Returns(job);
        successTaskMock.SetupGet(t => t.Id).Returns("task-success");
        successTaskMock.SetupGet(t => t.Status).Returns(JobTaskStatusEnum.Success);

        var execMock = new Mock<IJobTaskExecutionService>();
        execMock.SetupSequence(s => s.ExecuteAsync(It.IsAny<IJob>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(runningTaskMock.Object)
                .ReturnsAsync(successTaskMock.Object);

        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        await sut.ExecuteAsync(job);
        await sut.ExecuteAsync(job);

        // Act
        var result = sut.ReadByJob(job, JobTaskStatusEnum.Running).ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("task-running"));
    }

    [Test]
    public async Task ReadByJob_WithNoStatus_ReturnsAllTasksForJob()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job-all-status", null, method, false);

        var taskMock1 = new Mock<IJobTask>();
        taskMock1.SetupGet(t => t.Job).Returns(job);
        taskMock1.SetupGet(t => t.Id).Returns("t1");

        var taskMock2 = new Mock<IJobTask>();
        taskMock2.SetupGet(t => t.Job).Returns(job);
        taskMock2.SetupGet(t => t.Id).Returns("t2");

        var execMock = new Mock<IJobTaskExecutionService>();
        execMock.SetupSequence(s => s.ExecuteAsync(It.IsAny<IJob>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(taskMock1.Object)
                .ReturnsAsync(taskMock2.Object);

        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        await sut.ExecuteAsync(job);
        await sut.ExecuteAsync(job);

        // Act – keine Statuses übergeben
        var result = sut.ReadByJob(job).ToArray();

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
    }

    [Test]
    public async Task FindByTaskId_ExistingTask_ReturnsTask()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job-find", null, method, false);

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(t => t.Job).Returns(job);
        jobTaskMock.SetupGet(t => t.Id).Returns("task-find");

        var execMock = new Mock<IJobTaskExecutionService>();
        execMock.Setup(s => s.ExecuteAsync(It.IsAny<IJob>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobTaskMock.Object);

        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        await sut.ExecuteAsync(job);

        // Act
        var result = sut.FindByTaskId(job, "task-find");

        // Assert
        Assert.That(result, Is.SameAs(jobTaskMock.Object));
    }

    [Test]
    public void FindByTaskId_UnknownTaskId_ReturnsNull()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job-find-missing", null, method, false);

        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        // Act
        var result = sut.FindByTaskId(job, "does-not-exist");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindByTaskId_NullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        // Act & Assert
        Assert.That(() => sut.FindByTaskId(null!, "task1"), Throws.ArgumentNullException);
    }

    [Test]
    public void FindByTaskId_NullTaskId_ThrowsArgumentNullException()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job-null-taskid", null, method, false);

        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        // Act & Assert
        Assert.That(() => sut.FindByTaskId(job, null!), Throws.ArgumentNullException);
    }

    [Test]
    public void CancelTask_NullJobTask_ThrowsArgumentNullException()
    {
        // Arrange
        var execMock = new Mock<IJobTaskExecutionService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var sut = CreateSut(loggerMock, execMock);

        // Act & Assert
        Assert.That(() => sut.CancelTask(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void CancelTask_WithActiveTask_SignalsCancellationAndWritesJournalEntry()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job-cancel", null, method, false);
        var cts = new CancellationTokenSource();

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(t => t.Job).Returns(job);
        jobTaskMock.SetupGet(t => t.Id).Returns("task-cancel");
        jobTaskMock.SetupGet(t => t.CancellationToken).Returns(cts);

        var journalMock = new Mock<IJobTaskJournalService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var execMock = new Mock<IJobTaskExecutionService>();

        var sut = new JobTaskService(loggerMock.Object, execMock.Object, new InMemoryJobTaskStorage(), journalMock.Object);

        // Act
        sut.CancelTask(jobTaskMock.Object);

        // Assert
        Assert.That(cts.IsCancellationRequested, Is.True);
        journalMock.Verify(j => j.Append(jobTaskMock.Object, It.IsAny<JobTaskJournalEntry>()), Times.Once);
    }

    [Test]
    public void CancelTask_WithAlreadyDisposedCts_DoesNotThrow()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var job = new Job("job-cancel-disposed", null, method, false);
        var cts = new CancellationTokenSource();
        cts.Dispose();

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(t => t.Job).Returns(job);
        jobTaskMock.SetupGet(t => t.Id).Returns("task-cancel-disposed");
        jobTaskMock.SetupGet(t => t.CancellationToken).Returns(cts);

        var journalMock = new Mock<IJobTaskJournalService>();
        var loggerMock = new Mock<ILogger<JobTaskService>>();
        var execMock = new Mock<IJobTaskExecutionService>();

        var sut = new JobTaskService(loggerMock.Object, execMock.Object, new InMemoryJobTaskStorage(), journalMock.Object);

        // Act & Assert
        Assert.That(() => sut.CancelTask(jobTaskMock.Object), Throws.Nothing);
    }
}
