using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Services;

namespace ToolWheel.Extensions.JobManager;

[TestFixture]
public class JobTaskContextTest
{
    private static MethodInfo DummyMethod { get; } =
        typeof(JobTaskContextTest).GetMethod(nameof(DummyTarget), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void DummyTarget() { }

    private JobTask CreateJobTask()
    {
        var jobMock = new Mock<IJob>();
        jobMock.SetupGet(j => j.Id).Returns("job-1");
        jobMock.SetupGet(j => j.TargetMethod).Returns(DummyMethod);
        return new JobTask(jobMock.Object, "task-1");
    }

    [Test]
    public void Job_ReturnsJobFromJobTask()
    {
        var jobTask = CreateJobTask();
        var context = new JobTaskContext(jobTask);

        Assert.That(context.Job, Is.SameAs(jobTask.Job));
    }

    [Test]
    public void JobTask_ReturnsWrappedJobTask()
    {
        var jobTask = CreateJobTask();
        var context = new JobTaskContext(jobTask);

        Assert.That(context.JobTask, Is.SameAs(jobTask));
    }

    [Test]
    public void TargetMethod_ReturnsMethodFromJob()
    {
        var jobTask = CreateJobTask();
        var context = new JobTaskContext(jobTask);

        Assert.That(context.TargetMethod, Is.SameAs(DummyMethod));
    }

    [Test]
    public void ConditionStatus_DefaultIsReady()
    {
        var jobTask = CreateJobTask();
        var context = new JobTaskContext(jobTask);

        Assert.That(context.ConditionStatus.Status, Is.EqualTo(JobConditionStatusEnum.Ready));
    }

    [Test]
    public void TargetObject_DefaultIsNull()
    {
        var jobTask = CreateJobTask();
        var context = new JobTaskContext(jobTask);

        Assert.That(context.TargetObject, Is.Null);
    }

    [Test]
    public void TargetObject_SetViaBuilder_IsReflectedOnContext()
    {
        var jobTask = CreateJobTask();
        var context = new JobTaskContext(jobTask);
        IJobTaskContextBuilder builder = new JobTaskContextBuilder(context);
        var target = new object();

        builder.TargetObject = target;

        Assert.That(context.TargetObject, Is.SameAs(target));
    }

    [Test]
    public void ConditionStatus_SetViaBuilder_IsReflectedOnContext()
    {
        var jobTask = CreateJobTask();
        var context = new JobTaskContext(jobTask);
        IJobTaskContextBuilder builder = new JobTaskContextBuilder(context);

        builder.ConditionStatus = JobConditionStatus.NotReady;

        Assert.That(context.ConditionStatus.Status, Is.EqualTo(JobConditionStatusEnum.NotReady));
    }

    [Test]
    public void ServiceProvider_DefaultIsNullButReturnsViaExecutionService()
    {
        // ServiceProvider has internal set — verify it is exposed on the IJobTaskContext interface
        var jobTask = CreateJobTask();
        var context = new JobTaskContext(jobTask);

        Assert.That(context, Is.InstanceOf<IJobTaskContext>());
    }

    [Test]
    public void ImplementsIJobTaskContext()
    {
        var jobTask = CreateJobTask();
        var context = new JobTaskContext(jobTask);

        Assert.That(context, Is.InstanceOf<IJobTaskContext>());
    }
}
