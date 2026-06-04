using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Configuration;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class JobServiceTest
{
    private sealed class TestTarget
    {
        public void DoWork() { }
    }

    private static Mock<IJobDescription> CreateDescriptionMock(string id, System.Reflection.MethodInfo method, string name = "Name", string description = "")
    {
        var m = new Mock<IJobDescription>();
        m.SetupGet(d => d.Id).Returns(id);
        m.SetupGet(d => d.TargetObject).Returns(null!);
        m.SetupGet(d => d.TargetMethod).Returns(method);
        m.SetupGet(d => d.Name).Returns(name);
        m.SetupGet(d => d.Description).Returns(description);
        m.SetupGet(d => d.Features).Returns(System.Linq.Enumerable.Empty<IJobManagerFeature>());
        return m;
    }

    private static JobService CreateSut(Mock<ILogger<JobService>> logger, Mock<IJobTaskService> jobTaskService)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        return new JobService(logger.Object, jobTaskService.Object, serviceProvider.Object, new InMemoryJobStorage());
    }

    [Test]
    public void Add_NewJob_AddsAndReturnsJob()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var descMock = new Mock<IJobDescription>();
        var logger = new Mock<ILogger<JobService>>();
        descMock.SetupGet(d => d.Id).Returns("job1");
        descMock.SetupGet(d => d.TargetObject).Returns(new TestTarget());
        descMock.SetupGet(d => d.TargetMethod).Returns(method);
        descMock.SetupGet(d => d.Name).Returns("My Job");
        descMock.SetupGet(d => d.Description).Returns("Description");
        descMock.SetupGet(d => d.Features).Returns(System.Linq.Enumerable.Empty<IJobManagerFeature>());

        var jobTaskServiceMock = new Mock<IJobTaskService>();
        var sut = CreateSut(logger, jobTaskServiceMock);

        // Act
        var job = sut.Add(descMock.Object);

        // Assert
        Assert.That(job, Is.Not.Null);
        Assert.That(job.Id, Is.EqualTo("job1"));
        Assert.That(job.Name, Is.EqualTo("My Job"));
        Assert.That(job.Description, Is.EqualTo("Description"));
        Assert.That(job.TargetMethod, Is.EqualTo(method));
        Assert.That(sut.FindById("job1"), Is.Not.Null);
    }

    [Test]
    public void Add_DuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var logger = new Mock<ILogger<JobService>>();
        var descMock = CreateDescriptionMock("dup", method);

        var jobTaskServiceMock = new Mock<IJobTaskService>();
        var sut = CreateSut(logger, jobTaskServiceMock);

        // Act
        sut.Add(descMock.Object);

        // Assert
        Assert.That(() => sut.Add(descMock.Object), Throws.TypeOf<InvalidOperationException>()
            .With.Message.Contain("dup"));
    }

    [Test]
    public void FindById_NotFound_ReturnsNull()
    {
        var jobTaskServiceMock = new Mock<IJobTaskService>();
        var logger = new Mock<ILogger<JobService>>();
        var sut = CreateSut(logger, jobTaskServiceMock);

        Assert.That(sut.FindById("missing"), Is.Null);
    }

    [Test]
    public void ReadById_NotFound_ThrowsKeyNotFoundException()
    {
        var jobTaskServiceMock = new Mock<IJobTaskService>();
        var logger = new Mock<ILogger<JobService>>();
        var sut = CreateSut(logger, jobTaskServiceMock);

        Assert.That(() => sut.ReadById("missing"), Throws.TypeOf<KeyNotFoundException>()
            .With.Message.Contain("missing"));
    }

    [Test]
    public void Remove_ExistingJob_ReturnsTrue_ThenFalse()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var logger = new Mock<ILogger<JobService>>();
        var descMock = CreateDescriptionMock("toRemove", method);

        var jobTaskServiceMock = new Mock<IJobTaskService>();
        var sut = CreateSut(logger, jobTaskServiceMock);

        var job = sut.Add(descMock.Object);

        // Act & Assert
        Assert.That(sut.Remove(job), Is.True);
        Assert.That(sut.Remove(job), Is.False);
    }

    [Test]
    public void ReadAll_ReturnsAllAddedJobs()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var makeDesc = new System.Func<string, IJobDescription>(id => CreateDescriptionMock(id, method, id).Object);
        var logger = new Mock<ILogger<JobService>>();

        var jobTaskServiceMock = new Mock<IJobTaskService>();
        var sut = CreateSut(logger, jobTaskServiceMock);

        sut.Add(makeDesc("j1"));
        sut.Add(makeDesc("j2"));

        // Act
        var all = sut.ReadAll().Select(j => j.Id).ToList();

        // Assert
        Assert.That(all, Is.EquivalentTo(new[] { "j1", "j2" }));
    }

    [Test]
    public async Task ExecuteAsync_DelegatesToJobTaskServiceAndReturnsTask()
    {
        // Arrange
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.DoWork))!;
        var logger = new Mock<ILogger<JobService>>();
        var descMock = CreateDescriptionMock("exec", method, "exec");

        var jobTaskServiceMock = new Mock<IJobTaskService>();
        var sut = CreateSut(logger, jobTaskServiceMock);

        var job = sut.Add(descMock.Object);

        var jobTaskMock = new Mock<IJobTask>();
        jobTaskMock.SetupGet(t => t.Job).Returns(job);
        jobTaskMock.SetupGet(t => t.Id).Returns("task1");

        jobTaskServiceMock
            .Setup(s => s.ExecuteAsync(It.Is<IJob>(j => j.Id == job.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobTaskMock.Object)
            .Verifiable();

        // Act
        var result = await sut.ExecuteAsync(job);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo("task1"));
        jobTaskServiceMock.Verify();
    }
}
