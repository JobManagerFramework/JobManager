using System;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class JobExecutionStatisticsServiceTest
{
    private JobExecutionStatisticsService CreateService()
    {
        var loggerMock = new Mock<ILogger<JobExecutionStatisticsService>>();
        return new JobExecutionStatisticsService(loggerMock.Object);
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.That(() => new JobExecutionStatisticsService(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void GetStatistics_NoRecords_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetStatistics("unknown-job");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAllStatistics_NoRecords_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetAllStatistics();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Record_SingleSuccess_ReturnsCorrectStatistics()
    {
        // Arrange
        var service = CreateService();
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        service.Record("job-1", "TestJob", duration, JobTaskStatusEnum.Success);
        var stats = service.GetStatistics("job-1");

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.JobId, Is.EqualTo("job-1"));
        Assert.That(stats.JobName, Is.EqualTo("TestJob"));
        Assert.That(stats.TotalExecutions, Is.EqualTo(1));
        Assert.That(stats.SuccessCount, Is.EqualTo(1));
        Assert.That(stats.FailedCount, Is.EqualTo(0));
        Assert.That(stats.CanceledCount, Is.EqualTo(0));
        Assert.That(stats.NotReadyCount, Is.EqualTo(0));
        Assert.That(stats.ErrorRate, Is.EqualTo(0.0));
        Assert.That(stats.AverageExecutionTime, Is.EqualTo(duration));
        Assert.That(stats.MinExecutionTime, Is.EqualTo(duration));
        Assert.That(stats.MaxExecutionTime, Is.EqualTo(duration));
        Assert.That(stats.LastExecutionTimestamp, Is.Not.Null);
    }

    [Test]
    public void Record_MultipleMixed_CalculatesErrorRateCorrectly()
    {
        // Arrange
        var service = CreateService();

        // Act – 2 Erfolge, 1 Fehler, 1 Abbruch
        service.Record("job-2", "MixedJob", TimeSpan.FromMilliseconds(100), JobTaskStatusEnum.Success);
        service.Record("job-2", "MixedJob", TimeSpan.FromMilliseconds(200), JobTaskStatusEnum.Success);
        service.Record("job-2", "MixedJob", TimeSpan.FromMilliseconds(300), JobTaskStatusEnum.Failed);
        service.Record("job-2", "MixedJob", TimeSpan.FromMilliseconds(50), JobTaskStatusEnum.Canceled);

        var stats = service.GetStatistics("job-2");

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.TotalExecutions, Is.EqualTo(4));
        Assert.That(stats.SuccessCount, Is.EqualTo(2));
        Assert.That(stats.FailedCount, Is.EqualTo(1));
        Assert.That(stats.CanceledCount, Is.EqualTo(1));
        Assert.That(stats.ErrorRate, Is.EqualTo(0.25).Within(0.001));
    }

    [Test]
    public void Record_MultipleExecutions_CalculatesAverageMinMax()
    {
        // Arrange
        var service = CreateService();
        var d1 = TimeSpan.FromMilliseconds(100);
        var d2 = TimeSpan.FromMilliseconds(300);
        var d3 = TimeSpan.FromMilliseconds(200);

        // Act
        service.Record("job-3", "TimingJob", d1, JobTaskStatusEnum.Success);
        service.Record("job-3", "TimingJob", d2, JobTaskStatusEnum.Success);
        service.Record("job-3", "TimingJob", d3, JobTaskStatusEnum.Success);

        var stats = service.GetStatistics("job-3");

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.MinExecutionTime, Is.EqualTo(d1));
        Assert.That(stats.MaxExecutionTime, Is.EqualTo(d2));
        Assert.That(stats.AverageExecutionTime, Is.EqualTo(TimeSpan.FromMilliseconds(200)));
    }

    [Test]
    public void Record_NotReadyStatus_IncrementsNotReadyCount()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Record("job-4", "NotReadyJob", TimeSpan.Zero, JobTaskStatusEnum.NotReady);

        var stats = service.GetStatistics("job-4");

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.NotReadyCount, Is.EqualTo(1));
        Assert.That(stats.TotalExecutions, Is.EqualTo(1));
    }

    [Test]
    public void GetAllStatistics_MultipleJobs_ReturnsAll()
    {
        // Arrange
        var service = CreateService();
        service.Record("job-a", "JobA", TimeSpan.FromMilliseconds(50), JobTaskStatusEnum.Success);
        service.Record("job-b", "JobB", TimeSpan.FromMilliseconds(100), JobTaskStatusEnum.Failed);

        // Act
        var all = service.GetAllStatistics().ToList();

        // Assert
        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all.Select(s => s.JobId), Is.EquivalentTo(new[] { "job-a", "job-b" }));
    }

    [Test]
    public void Reset_RemovesStatisticsForJob()
    {
        // Arrange
        var service = CreateService();
        service.Record("job-r", "ResetJob", TimeSpan.FromMilliseconds(50), JobTaskStatusEnum.Success);

        // Act
        service.Reset("job-r");

        // Assert
        Assert.That(service.GetStatistics("job-r"), Is.Null);
    }

    [Test]
    public void ResetAll_RemovesAllStatistics()
    {
        // Arrange
        var service = CreateService();
        service.Record("job-x", "JobX", TimeSpan.FromMilliseconds(50), JobTaskStatusEnum.Success);
        service.Record("job-y", "JobY", TimeSpan.FromMilliseconds(100), JobTaskStatusEnum.Success);

        // Act
        service.ResetAll();

        // Assert
        Assert.That(service.GetAllStatistics(), Is.Empty);
    }

    [Test]
    public void Record_UpdatesJobName_WhenChanged()
    {
        // Arrange
        var service = CreateService();
        service.Record("job-n", "OldName", TimeSpan.FromMilliseconds(50), JobTaskStatusEnum.Success);

        // Act
        service.Record("job-n", "NewName", TimeSpan.FromMilliseconds(100), JobTaskStatusEnum.Success);
        var stats = service.GetStatistics("job-n");

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.JobName, Is.EqualTo("NewName"));
        Assert.That(stats.TotalExecutions, Is.EqualTo(2));
    }

    [Test]
    public void Record_AllFailed_ErrorRateIsOne()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Record("job-f", "FailJob", TimeSpan.FromMilliseconds(10), JobTaskStatusEnum.Failed);
        service.Record("job-f", "FailJob", TimeSpan.FromMilliseconds(20), JobTaskStatusEnum.Failed);

        var stats = service.GetStatistics("job-f");

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.ErrorRate, Is.EqualTo(1.0).Within(0.001));
    }
}
