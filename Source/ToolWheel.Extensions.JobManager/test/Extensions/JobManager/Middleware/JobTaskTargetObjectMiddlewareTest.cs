using System.Reflection;
using Moq;
using NUnit.Framework;

namespace ToolWheel.Extensions.JobManager.Middleware;

[TestFixture]
public class JobTaskTargetObjectMiddlewareTest
{
    // Hilfsklasse mit statischer und Instanz-Methode als Testziel
    private class TestTarget
    {
        public TestTarget() { }

        public void InstanceMethod() { }

        public static void StaticMethod() { }
    }

    [Test]
    public async Task InvokeAsync_WithStaticMethod_DoesNotCreateTargetObject_AndCallsNext()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var middleware = new JobTaskTargetObjectMiddleware(serviceProviderMock.Object);

        var contextMock = new Mock<IJobTaskContextBuilder>();
        // TargetMethod ist readonly -> SetupGet; TargetObject ist beschreibbar -> SetupProperty
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.StaticMethod), BindingFlags.Public | BindingFlags.Static)!;
        contextMock.SetupGet(c => c.TargetMethod).Returns(method);
        contextMock.SetupProperty(c => c.TargetObject, null);

        var nextCalled = false;
        Func<Task> next = () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        var task = middleware.InvokeAsync(contextMock.Object, next, CancellationToken.None);
        await task.ConfigureAwait(false);

        // Assert
        Assert.That(nextCalled, Is.True, "Das 'next' delegate muss aufgerufen werden.");
        Assert.That(contextMock.Object.TargetObject, Is.Null, "Bei statischer Methode darf kein TargetObject erstellt werden.");
    }

    [Test]
    public async Task InvokeAsync_WithInstanceMethod_CreatesTargetObject_WhenNull()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var middleware = new JobTaskTargetObjectMiddleware(serviceProviderMock.Object);

        var contextMock = new Mock<IJobTaskContextBuilder>();
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.InstanceMethod), BindingFlags.Public | BindingFlags.Instance)!;
        contextMock.SetupGet(c => c.TargetMethod).Returns(method);
        contextMock.SetupProperty(c => c.TargetObject, null);

        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.UseSingletonInstance).Returns(false);
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);

        var nextCalled = false;
        Func<Task> next = () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        var task = middleware.InvokeAsync(contextMock.Object, next, CancellationToken.None);
        await task.ConfigureAwait(false);

        // Assert
        Assert.That(nextCalled, Is.True);
        Assert.That(contextMock.Object.TargetObject, Is.Not.Null, "Bei Instanz-Methode muss ein TargetObject erstellt werden.");
        Assert.That(contextMock.Object.TargetObject, Is.InstanceOf<TestTarget>());
    }

    [Test]
    public async Task InvokeAsync_WithInstanceMethod_DoesNotOverwriteExistingTargetObject()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var middleware = new JobTaskTargetObjectMiddleware(serviceProviderMock.Object);

        var contextMock = new Mock<IJobTaskContextBuilder>();
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.InstanceMethod), BindingFlags.Public | BindingFlags.Instance)!;
        contextMock.SetupGet(c => c.TargetMethod).Returns(method);

        var existing = new TestTarget();
        contextMock.SetupProperty(c => c.TargetObject, existing);

        var nextCalled = false;
        Func<Task> next = () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        var task = middleware.InvokeAsync(contextMock.Object, next, CancellationToken.None);
        await task.ConfigureAwait(false);

        // Assert
        Assert.That(nextCalled, Is.True);
        Assert.That(ReferenceEquals(contextMock.Object.TargetObject, existing), Is.True, "Existierendes TargetObject darf nicht überschrieben werden.");
    }

    [Test]
    public async Task InvokeAsync_ReturnsAndAwaitsNextTask()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var middleware = new JobTaskTargetObjectMiddleware(serviceProviderMock.Object);

        var contextMock = new Mock<IJobTaskContextBuilder>();
        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.InstanceMethod), BindingFlags.Public | BindingFlags.Instance)!;
        contextMock.SetupGet(c => c.TargetMethod).Returns(method);
        contextMock.SetupProperty(c => c.TargetObject, null);

        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.UseSingletonInstance).Returns(false);
        contextMock.SetupGet(c => c.Job).Returns(jobMock.Object);

        var tcs = new TaskCompletionSource<bool>();
        var nextCalled = false;
        Func<Task> next = async () =>
        {
            nextCalled = true;
            await Task.Delay(10).ConfigureAwait(false);
            tcs.SetResult(true);
        };

        // Act
        var invokeTask = middleware.InvokeAsync(contextMock.Object, next, CancellationToken.None);
        await invokeTask.ConfigureAwait(false);

        // Assert
        Assert.That(nextCalled, Is.True);
        Assert.That(tcs.Task.IsCompleted, Is.True, "Das vom 'next' zurückgegebene Task muss abgeschlossen sein und awaitet werden.");
    }

    [Test]
    public async Task InvokeAsync_WithSingletonInstance_ReusesSameInstance()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var middleware = new JobTaskTargetObjectMiddleware(serviceProviderMock.Object);

        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.InstanceMethod), BindingFlags.Public | BindingFlags.Instance)!;

        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns("singleton-job-id");
        jobMock.SetupGet(j => j.UseSingletonInstance).Returns(true);

        var context1Mock = new Mock<IJobTaskContextBuilder>();
        context1Mock.SetupGet(c => c.TargetMethod).Returns(method);
        context1Mock.SetupProperty(c => c.TargetObject, null);
        context1Mock.SetupGet(c => c.Job).Returns(jobMock.Object);

        var context2Mock = new Mock<IJobTaskContextBuilder>();
        context2Mock.SetupGet(c => c.TargetMethod).Returns(method);
        context2Mock.SetupProperty(c => c.TargetObject, null);
        context2Mock.SetupGet(c => c.Job).Returns(jobMock.Object);

        Func<Task> next = () => Task.CompletedTask;

        // Act
        await middleware.InvokeAsync(context1Mock.Object, next, CancellationToken.None).ConfigureAwait(false);
        await middleware.InvokeAsync(context2Mock.Object, next, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(context1Mock.Object.TargetObject, Is.Not.Null);
        Assert.That(context2Mock.Object.TargetObject, Is.Not.Null);
        Assert.That(ReferenceEquals(context1Mock.Object.TargetObject, context2Mock.Object.TargetObject), Is.True, 
            "Bei aktiviertem Singleton-Modus muss dieselbe Instanz wiederverwendet werden.");
    }

    [Test]
    public async Task InvokeAsync_WithoutSingletonInstance_CreatesNewInstances()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var middleware = new JobTaskTargetObjectMiddleware(serviceProviderMock.Object);

        var method = typeof(TestTarget).GetMethod(nameof(TestTarget.InstanceMethod), BindingFlags.Public | BindingFlags.Instance)!;

        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns("non-singleton-job-id");
        jobMock.SetupGet(j => j.UseSingletonInstance).Returns(false);

        var context1Mock = new Mock<IJobTaskContextBuilder>();
        context1Mock.SetupGet(c => c.TargetMethod).Returns(method);
        context1Mock.SetupProperty(c => c.TargetObject, null);
        context1Mock.SetupGet(c => c.Job).Returns(jobMock.Object);

        var context2Mock = new Mock<IJobTaskContextBuilder>();
        context2Mock.SetupGet(c => c.TargetMethod).Returns(method);
        context2Mock.SetupProperty(c => c.TargetObject, null);
        context2Mock.SetupGet(c => c.Job).Returns(jobMock.Object);

        Func<Task> next = () => Task.CompletedTask;

        // Act
        await middleware.InvokeAsync(context1Mock.Object, next, CancellationToken.None).ConfigureAwait(false);
        await middleware.InvokeAsync(context2Mock.Object, next, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(context1Mock.Object.TargetObject, Is.Not.Null);
        Assert.That(context2Mock.Object.TargetObject, Is.Not.Null);
        Assert.That(ReferenceEquals(context1Mock.Object.TargetObject, context2Mock.Object.TargetObject), Is.False, 
            "Ohne Singleton-Modus müssen verschiedene Instanzen erstellt werden.");
    }
}
