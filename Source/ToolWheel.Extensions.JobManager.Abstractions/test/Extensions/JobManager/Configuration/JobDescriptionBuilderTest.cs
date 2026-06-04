using System;
using System.Reflection;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Configuration;

namespace ToolWheel.Extensions.JobManager.Configuration;

[TestFixture]
public class JobDescriptionBuilderTest
{
    private static void DummyTargetMethod()
    {
    }

    private sealed class SampleFeature : IJobManagerFeature
    {
        public int Value { get; set; }

        public void Apply(IServiceProvider serviceProvider, IJobDescription jobDescription, IJob job)
        {
        }
    }

    [Test]
    public void Description_SetsJobDescriptionDescription()
    {
        var method = typeof(JobDescriptionBuilderTest).GetMethod(nameof(DummyTargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var jobDescription = new JobDescription(null, method);
        var builder = new JobDescriptionBuilder(jobDescription);

        var returned = builder.Description("My description");

        Assert.That(ReferenceEquals(builder, returned), Is.True);
        Assert.That(jobDescription.Description, Is.EqualTo("My description"));
    }

    [Test]
    public void Disabled_And_Enabled_ToggleJobDescriptionEnabled()
    {
        var method = typeof(JobDescriptionBuilderTest).GetMethod(nameof(DummyTargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var jobDescription = new JobDescription(null, method);
        var builder = new JobDescriptionBuilder(jobDescription);

        // Default is enabled
        Assert.That(jobDescription.Enabled, Is.True);

        builder.Disabled();
        Assert.That(jobDescription.Enabled, Is.False);

        builder.Enabled();
        Assert.That(jobDescription.Enabled, Is.True);
    }

    [Test]
    public void Id_ValidValue_SetsId_And_ReturnsBuilder()
    {
        var method = typeof(JobDescriptionBuilderTest).GetMethod(nameof(DummyTargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var jobDescription = new JobDescription(null, method);
        var builder = new JobDescriptionBuilder(jobDescription);

        var returned = builder.Id("custom-id-123");

        Assert.That(ReferenceEquals(builder, returned), Is.True);
        Assert.That(jobDescription.Id, Is.EqualTo("custom-id-123"));
    }

    [Test]
    public void Id_NullOrWhiteSpace_ThrowsArgumentException()
    {
        var method = typeof(JobDescriptionBuilderTest).GetMethod(nameof(DummyTargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var jobDescription = new JobDescription(null, method);
        var builder = new JobDescriptionBuilder(jobDescription);

        Assert.That(() => builder.Id(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.Id(string.Empty), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.Id("   "), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Name_ValidValue_SetsName_And_ReturnsBuilder()
    {
        var method = typeof(JobDescriptionBuilderTest).GetMethod(nameof(DummyTargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var jobDescription = new JobDescription(null, method);
        var builder = new JobDescriptionBuilder(jobDescription);

        var returned = builder.Name("Display Name");

        Assert.That(ReferenceEquals(builder, returned), Is.True);
        Assert.That(jobDescription.Name, Is.EqualTo("Display Name"));
    }

    [Test]
    public void Name_NullOrWhiteSpace_ThrowsArgumentException()
    {
        var method = typeof(JobDescriptionBuilderTest).GetMethod(nameof(DummyTargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var jobDescription = new JobDescription(null, method);
        var builder = new JobDescriptionBuilder(jobDescription);

        Assert.That(() => builder.Name(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.Name(string.Empty), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.Name("   "), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void WithFeature_CreatesFeature_And_AppliesAction()
    {
        var method = typeof(JobDescriptionBuilderTest).GetMethod(nameof(DummyTargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var jobDescription = new JobDescription(null, method);
        var builder = new JobDescriptionBuilder(jobDescription);

        var returned = builder.WithFeature<SampleFeature>(f => f.Value = 42);

        Assert.That(ReferenceEquals(builder, returned), Is.True);

        var feature = jobDescription.GetFeature<SampleFeature>();
        Assert.That(feature, Is.Not.Null);
        Assert.That(feature!.Value, Is.EqualTo(42));
    }

    [Test]
    public void AsSingleton_SetsUseSingletonInstanceToTrue_And_ReturnsBuilder()
    {
        var method = typeof(JobDescriptionBuilderTest).GetMethod(nameof(DummyTargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var jobDescription = new JobDescription(null, method);
        var builder = new JobDescriptionBuilder(jobDescription);

        Assert.That(jobDescription.UseSingletonInstance, Is.False);

        var returned = builder.AsSingleton();

        Assert.That(ReferenceEquals(builder, returned), Is.True);
        Assert.That(jobDescription.UseSingletonInstance, Is.True);
    }
}
