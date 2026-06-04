using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class JobTaskJournalServiceTest
{
    private static Mock<IJobTask> CreateJobTaskMock(string taskId = "task-1")
    {
        var mock = new Mock<IJobTask>();
        mock.SetupGet(t => t.Id).Returns(taskId);
        return mock;
    }

    private static JobTaskJournalService CreateSut(IJobTaskJournalStorage? storage = null)
    {
        return new JobTaskJournalService(
            storage ?? new InMemoryJobTaskJournalStorage(),
            new Mock<ILogger<JobTaskJournalService>>().Object);
    }

    [Test]
    public void Constructor_NullStorage_ThrowsArgumentNullException()
    {
        var loggerMock = new Mock<ILogger<JobTaskJournalService>>();

        Assert.That(
            () => new JobTaskJournalService(null!, loggerMock.Object),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("storage"));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new JobTaskJournalService(new InMemoryJobTaskJournalStorage(), null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("logger"));
    }

    [Test]
    public void GetEntries_WhenNoEntriesAppended_ReturnsEmpty()
    {
        var sut = CreateSut();
        var jobTask = CreateJobTaskMock().Object;

        Assert.That(sut.GetEntries(jobTask), Is.Empty);
    }

    [Test]
    public void Append_ThenGetEntries_ReturnsSameEntry()
    {
        var sut = CreateSut();
        var jobTask = CreateJobTaskMock().Object;
        var entry = new JobTaskJournalEntry(
            System.DateTimeOffset.UtcNow,
            Microsoft.Extensions.Logging.LogLevel.Information,
            "test message",
            null);

        sut.Append(jobTask, entry);

        var entries = sut.GetEntries(jobTask);
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries.Single(), Is.SameAs(entry));
    }

    [Test]
    public void Append_MultipleEntries_AllStoredInOrder()
    {
        var sut = CreateSut();
        var jobTask = CreateJobTaskMock().Object;

        var entry1 = new JobTaskJournalEntry(System.DateTimeOffset.UtcNow, Microsoft.Extensions.Logging.LogLevel.Debug, "first", null);
        var entry2 = new JobTaskJournalEntry(System.DateTimeOffset.UtcNow, Microsoft.Extensions.Logging.LogLevel.Warning, "second", null);

        sut.Append(jobTask, entry1);
        sut.Append(jobTask, entry2);

        var entries = sut.GetEntries(jobTask).ToArray();
        Assert.That(entries, Has.Length.EqualTo(2));
        Assert.That(entries[0], Is.SameAs(entry1));
        Assert.That(entries[1], Is.SameAs(entry2));
    }

    [Test]
    public void GetEntries_DifferentTasks_AreIsolated()
    {
        var sut = CreateSut();
        var task1 = CreateJobTaskMock("task-A").Object;
        var task2 = CreateJobTaskMock("task-B").Object;

        var entry = new JobTaskJournalEntry(System.DateTimeOffset.UtcNow, Microsoft.Extensions.Logging.LogLevel.Information, "only for task-A", null);
        sut.Append(task1, entry);

        Assert.That(sut.GetEntries(task1), Has.Count.EqualTo(1));
        Assert.That(sut.GetEntries(task2), Is.Empty);
    }

    [Test]
    public void Append_DelegatesToStorage()
    {
        var storageMock = new Mock<IJobTaskJournalStorage>();
        var sut = CreateSut(storageMock.Object);
        var jobTask = CreateJobTaskMock("t1").Object;
        var entry = new JobTaskJournalEntry(System.DateTimeOffset.UtcNow, Microsoft.Extensions.Logging.LogLevel.Error, "err", null);

        sut.Append(jobTask, entry);

        storageMock.Verify(s => s.Append("t1", entry), Times.Once);
    }

    [Test]
    public void GetEntries_DelegatesToStorage()
    {
        var expected = new[] { new JobTaskJournalEntry(System.DateTimeOffset.UtcNow, Microsoft.Extensions.Logging.LogLevel.Information, "x", null) };
        var storageMock = new Mock<IJobTaskJournalStorage>();
        storageMock.Setup(s => s.GetEntries("t2")).Returns(expected);

        var sut = CreateSut(storageMock.Object);
        var jobTask = CreateJobTaskMock("t2").Object;

        var result = sut.GetEntries(jobTask);

        Assert.That(result, Is.SameAs(expected));
        storageMock.Verify(s => s.GetEntries("t2"), Times.Once);
    }
}
