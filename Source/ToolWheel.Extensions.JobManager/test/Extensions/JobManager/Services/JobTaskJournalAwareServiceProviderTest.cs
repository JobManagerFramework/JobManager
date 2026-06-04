using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Services;

/// <summary>
/// Tests for <c>JobTaskJournalAwareServiceProvider</c> via reflection,
/// because the class is internal and not directly accessible from the test project.
/// </summary>
[TestFixture]
public class JobTaskJournalAwareServiceProviderTest
{
    private static readonly Type ProviderType =
        typeof(JobTaskExecutionService).Assembly
            .GetType("ToolWheel.Extensions.JobManager.Services.JobTaskJournalAwareServiceProvider")!;

    private IServiceProvider CreateProvider(IServiceProvider inner, ILogger journal)
    {
        return (IServiceProvider)Activator.CreateInstance(ProviderType, inner, journal)!;
    }

    [Test]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var journalMock = new Mock<ILogger>();

        var ex = Assert.Throws<TargetInvocationException>(
            () => Activator.CreateInstance(ProviderType, null!, journalMock.Object));

        Assert.That(ex!.InnerException, Is.InstanceOf<ArgumentNullException>());
        Assert.That(((ArgumentNullException)ex.InnerException!).ParamName, Is.EqualTo("inner"));
    }

    [Test]
    public void Constructor_NullJournal_ThrowsArgumentNullException()
    {
        var innerMock = new Mock<IServiceProvider>();

        var ex = Assert.Throws<TargetInvocationException>(
            () => Activator.CreateInstance(ProviderType, innerMock.Object, null!));

        Assert.That(ex!.InnerException, Is.InstanceOf<ArgumentNullException>());
        Assert.That(((ArgumentNullException)ex.InnerException!).ParamName, Is.EqualTo("journal"));
    }

    [Test]
    public void GetService_ILogger_ReturnsJournal()
    {
        var innerMock = new Mock<IServiceProvider>();
        var journalMock = new Mock<ILogger>();
        var provider = CreateProvider(innerMock.Object, journalMock.Object);

        var result = provider.GetService(typeof(ILogger));

        Assert.That(result, Is.SameAs(journalMock.Object));
        innerMock.Verify(sp => sp.GetService(It.IsAny<Type>()), Times.Never);
    }

    [Test]
    public void GetService_ILoggerOfT_ReturnsAdapterWrappingJournal()
    {
        var innerMock = new Mock<IServiceProvider>();
        var journalMock = new Mock<ILogger>();
        var provider = CreateProvider(innerMock.Object, journalMock.Object);

        var result = provider.GetService(typeof(ILogger<string>));

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ILogger<string>>());
        innerMock.Verify(sp => sp.GetService(It.IsAny<Type>()), Times.Never);
    }

    [Test]
    public void GetService_ILoggerOfT_AdapterForwardsCalls()
    {
        var innerMock = new Mock<IServiceProvider>();
        var journalMock = new Mock<ILogger>();
        journalMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var provider = CreateProvider(innerMock.Object, journalMock.Object);

        var result = (ILogger<string>)provider.GetService(typeof(ILogger<string>))!;
        result.LogInformation("test");

        journalMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void GetService_OtherType_DelegatesToInnerProvider()
    {
        var innerMock = new Mock<IServiceProvider>();
        var journalMock = new Mock<ILogger>();
        var expected = new object();
        innerMock.Setup(sp => sp.GetService(typeof(string))).Returns(expected);
        var provider = CreateProvider(innerMock.Object, journalMock.Object);

        var result = provider.GetService(typeof(string));

        Assert.That(result, Is.SameAs(expected));
    }

    [Test]
    public void GetService_OtherType_NotRegistered_ReturnsNull()
    {
        var innerMock = new Mock<IServiceProvider>();
        var journalMock = new Mock<ILogger>();
        innerMock.Setup(sp => sp.GetService(typeof(Guid))).Returns((object?)null);
        var provider = CreateProvider(innerMock.Object, journalMock.Object);

        var result = provider.GetService(typeof(Guid));

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetService_DifferentILoggerOfT_EachCallCreatesNewAdapter()
    {
        var innerMock = new Mock<IServiceProvider>();
        var journalMock = new Mock<ILogger>();
        var provider = CreateProvider(innerMock.Object, journalMock.Object);

        var result1 = provider.GetService(typeof(ILogger<string>));
        var result2 = provider.GetService(typeof(ILogger<int>));

        Assert.That(result1, Is.InstanceOf<ILogger<string>>());
        Assert.That(result2, Is.InstanceOf<ILogger<int>>());
        Assert.That(result1, Is.Not.SameAs(result2));
    }
}
