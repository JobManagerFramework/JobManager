using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class JobOptionServiceTest
{
    private JobOptionService CreateService()
    {
        var storage = new InMemoryJobOptionStorage();
        return new JobOptionService(storage);
    }

    private static Mock<IJob> CreateJobMock()
    {
        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns(Guid.NewGuid().ToString());
        return jobMock;
    }

    [Test]
    public void Add_WithOption_StoresOptionForJob()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        var option = new TestOption { Value = "test" };

        // Act
        service.Add(job, option);

        // Assert
        var retrieved = service.Get<TestOption>(job);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("test"));
    }

    [Test]
    public void Add_Generic_StoresOptionForJob()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        var option = new TestOption { Value = "generic" };

        // Act
        service.Add(job, option);

        // Assert
        var retrieved = service.Get<TestOption>(job);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("generic"));
    }

    [Test]
    public void Add_MultipleOptions_StoresMultipleOptions()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        var option1 = new TestOption { Value = "option1" };
        var option2 = new AnotherTestOption { Data = "option2" };

        // Act
        service.Add(job, option1);
        service.Add(job, option2);

        // Assert
        var all = service.GetAll(job);
        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(service.Get<TestOption>(job)!.Value, Is.EqualTo("option1"));
        Assert.That(service.Get<AnotherTestOption>(job)!.Data, Is.EqualTo("option2"));
    }

    [Test]
    public void Add_ReplaceExisting_OverwritesOption()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        var option1 = new TestOption { Value = "first" };
        var option2 = new TestOption { Value = "second" };

        // Act
        service.Add(job, option1);
        service.Add(job, option2);

        // Assert
        var retrieved = service.Get<TestOption>(job);
        Assert.That(retrieved!.Value, Is.EqualTo("second"));
    }

    [Test]
    public void Update_WithExistingOption_UpdatesOption()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        var option1 = new TestOption { Value = "first" };
        var option2 = new TestOption { Value = "updated" };
        service.Add(job, option1);

        // Act
        var result = service.Update(job, option2);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(service.Get<TestOption>(job)!.Value, Is.EqualTo("updated"));
    }

    [Test]
    public void Update_WithoutExistingOption_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        var option = new TestOption { Value = "test" };

        // Act
        var result = service.Update(job, option);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Remove_WithExistingOption_RemovesOption()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        var option = new TestOption { Value = "test" };
        service.Add(job, option);

        // Act
        var result = service.Remove<TestOption>(job);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(service.Get<TestOption>(job), Is.Null);
    }

    [Test]
    public void Remove_WithoutExistingOption_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;

        // Act
        var result = service.Remove<TestOption>(job);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Get_WithExistingOption_ReturnsOption()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        var option = new TestOption { Value = "found" };
        service.Add(job, option);

        // Act
        var retrieved = service.Get<TestOption>(job);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("found"));
    }

    [Test]
    public void Get_WithoutExistingOption_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;

        // Act
        var retrieved = service.Get<TestOption>(job);

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void Contains_WithExistingOption_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        var option = new TestOption { Value = "test" };
        service.Add(job, option);

        // Act
        var result = service.Contains<TestOption>(job);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_WithoutExistingOption_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;

        // Act
        var result = service.Contains<TestOption>(job);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetAll_WithMultipleOptions_ReturnsAllOptions()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;
        service.Add(job, new TestOption { Value = "option1" });
        service.Add(job, new AnotherTestOption { Data = "option2" });

        // Act
        var all = service.GetAll(job);

        // Assert
        Assert.That(all, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetAll_WithNoOptions_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;

        // Act
        var all = service.GetAll(job);

        // Assert
        Assert.That(all, Is.Empty);
    }

    [Test]
    public void Add_WithNullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Add(null!, option));
    }

    [Test]
    public void Add_WithNullOption_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var job = CreateJobMock().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Add(job, (TestOption)null!));
    }

    // Test helpers
    private class TestOption
    {
        public string Value { get; set; } = string.Empty;
    }

    private class AnotherTestOption
    {
        public string Data { get; set; } = string.Empty;
    }
}
