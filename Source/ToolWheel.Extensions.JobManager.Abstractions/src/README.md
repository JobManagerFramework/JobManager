# ToolWheel.Extensions.JobManager.Abstractions

Public API, contracts and DTOs for the ToolWheel Job Manager. This package contains **no implementations** – consumers and extension packages reference these abstractions.

## Package Info

| Property | Value |
|---|---|
| Target Framework | `net8.0` |
| NuGet Package ID | `ToolWheel.Extensions.JobManager.Abstractions` |
| Dependencies | `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions` |

---

## Core Interfaces

### `IJob`

Describes a registered job – an immutable identifier, the `MethodInfo` to invoke and optional metadata.

```csharp
public interface IJob
{
    string Id { get; init; }
    MethodInfo TargetMethod { get; init; }
    object? TargetObject { get; init; }
    string Name { get; }
    string Description { get; }
    bool Enabled { get; }
    bool UseSingletonInstance { get; }
    ILogger? JobLogger { get; }
}
```

| Property | Description |
|---|---|
| `Id` | Unique identifier used throughout the system. |
| `TargetMethod` | The method to invoke when the job executes. |
| `TargetObject` | The instance to invoke the method on. `null` means the object is resolved from DI at runtime. |
| `UseSingletonInstance` | When `true`, the same target instance is reused across executions instead of creating a new one each time. |
| `JobLogger` | Optional per-job logger; journal entries are forwarded to it. |

---

### `IJobTask`

Runtime handle for a single execution of a job.

```csharp
public interface IJobTask
{
    IJob Job { get; }
    string Id { get; }
    JobTaskStatusEnum Status { get; }
    CancellationTokenSource CancellationToken { get; }
    Task? Task { get; }
    object? Result { get; }
    IReadOnlyCollection<JobTaskJournalEntry> Journal { get; }
}
```

`JobTaskStatusEnum` values: `Pending`, `Running`, `Success`, `Canceled`, `Failed`, `NotReady`.

---

### `IJobService`

Central service for registering, querying and executing jobs.

```csharp
public interface IJobService
{
    IJob Add(IJobDescription jobDescription);
    IJob? FindById(string jobId);
    IJob ReadById(string jobId);
    IEnumerable<IJob> ReadAll();
    bool Remove(IJob job);
    ValueTask<IJobTask> ExecuteAsync(IJob job, CancellationToken cancellationToken = default);
}
```

After a job is registered, `Add` calls `IJobManagerFeature.Apply` on every feature attached to the `IJobDescription`. This is the unified hook for extension packages to register job-specific configuration (triggers, retry policies, …) both at startup and at runtime.

---

### `IJobTaskService`

Manages the lifecycle of `IJobTask` instances at runtime.

```csharp
public interface IJobTaskService
{
    ValueTask<IJobTask> ExecuteAsync(IJob job, CancellationToken cancellationToken = default);
    void Remove(IJobTask jobTask);
    IEnumerable<IJobTask> ReadAll();
    IEnumerable<IJobTask> ReadByJob(IJob job);
    IEnumerable<IJobTask> ReadByJob(IJob job, params JobTaskStatusEnum[] status);
    Task CancelAllAndWaitAsync(CancellationToken cancellationToken = default);
}
```

`CancelAllAndWaitAsync` signals cancellation on all tracked tasks and awaits their completion – used during graceful shutdown.

---

### `IJobExecutionConditionService`

Service for storing and retrieving execution condition features per job. Features are keyed by job and feature type so that each job holds at most one instance per feature type.

```csharp
public interface IJobExecutionConditionService
{
    void Add(IJob job, IJobManagerFeature feature);
    void Add<T>(IJob job, T feature) where T : IJobManagerFeature;
    void Update<T>(IJob job, Action<T> update) where T : IJobManagerFeature;
    T? Get<T>(IJob job) where T : IJobManagerFeature;
    IReadOnlyDictionary<Type, IJobManagerFeature> GetAll(IJob job);
    bool Remove(IJob job, Type featureType);
}
```

This service is the unified interface for extension packages to store feature-specific data per job. It allows third-party libraries to attach their own feature instances without modifying this service.

---

### `IJobTaskJournalService`

Service responsible for recording and querying journal entries produced during job task executions.

```csharp
public interface IJobTaskJournalService
{
    void Append(IJobTask jobTask, JobTaskJournalEntry entry);
    IReadOnlyCollection<JobTaskJournalEntry> GetEntries(IJobTask jobTask);
}
```

All log entries written during a job execution are routed through this service. When a per-job logger is attached, entries are also forwarded to it.

---

### `IJobExecutionStatisticsService`

Provides access to aggregated execution statistics for registered jobs.

```csharp
public interface IJobExecutionStatisticsService
{
    JobExecutionStatistics? GetStatistics(string jobId);
    IEnumerable<JobExecutionStatistics> GetAllStatistics();
    void Record(string jobId, string jobName, TimeSpan duration, JobTaskStatusEnum status);
    void Reset(string jobId);
    void ResetAll();
}
```

Tracks execution count, success/failure rates, average duration and last execution time for each job.

---

## Configuration API

### `IJobDescriptionCollection`

Typed collection with extension methods for convenient job registration:

```csharp
// Explicit ID + delegate
collection.Add("my-job", worker.DoWork)
    .Name("My Job")
    .Description("Does the work")
    .Enabled();

// Auto-generated ID
collection.Add(worker.DoWork).Name("My Job");

// Expression – method resolved via reflection, ID auto-generated
collection.Add<MyWorker>(w => w.DoWork).Name("Expression Job");

// Expression with explicit ID
collection.Add<MyWorker>("my-job", w => (Delegate)w.DoWork).Name("Expression Job");
```

---

### `IJobDescriptionBuilder`

Fluent builder for configuring a single job description:

```csharp
collection.Add("job-1", handler.Run)
    .Id("custom-id")          // override the job ID
    .Name("Custom Name")
    .Description("Processes items")
    .Enabled()                // set Enabled = true
    .Disabled()               // set Enabled = false
    .AsSingleton()            // reuse the same target instance
    .WithLogger(myLogger)     // attach a per-job logger
    .WithFeature<MyFeature>(f => f.Setting = true);  // attach an extension feature
```

---

### `IJobManagerConfigurationBuilder`

Top-level builder used inside `AddJobManager(configure => …)`:

```csharp
services.AddJobManager(configure =>
{
    configure
        .ConfigureJobs(jobs => { /* add jobs */ })
        .AddExecutionCondition<MyCondition>()
        .AddExecutionConditionController<MyController>()
        .AddMiddleware<MyMiddleware>()
        .ConfigureServices(svc => svc.AddSingleton<IMyService, MyService>())
        .ConfigureService(ServiceLifetime.Singleton, (sp, cfg) => new MyService(cfg));

    configure.SetFeature(new MyFeature());
    var feature = configure.GetFeature<MyFeature>();
});
```

---

## Extensibility Interfaces

### `IJobManagerFeature`

Base interface for feature objects attached to job descriptions or the configuration builder. Override `Apply` to self-register job-specific data with the appropriate runtime service when `IJobService.Add` is called.

```csharp
public interface IJobManagerFeature
{
    // Default no-op – override to register with runtime services.
    void Apply(IServiceProvider serviceProvider, IJobDescription jobDescription, IJob job) { }
}
```

### `IAutoFeatureConfigurator`

Implementations are auto-discovered at startup by `AddJobManager`. Each is called once to register **services** into the DI container (middleware, conditions, singletons). Per-job data is handled separately through `IJobManagerFeature.Apply`.

```csharp
public class MyConfigurator : IAutoFeatureConfigurator
{
    public void AutoConfigure(IJobManagerConfigurationBuilder builder)
    {
        builder.ConfigureServices(services =>
            services.AddSingleton<IMyService, MyService>());
        builder.AddMiddleware<MyMiddleware>();
    }
}
```

## Storage Abstractions

The framework defines storage contracts to enable custom persistence implementations beyond the default in-memory stores.

### `IJobStorage`

Central abstraction for job persistence.

```csharp
public interface IJobStorage
{
    void Add(IJob job);
    IJob? FindById(string jobId);
    bool Remove(string jobId);
    IReadOnlyCollection<IJob> ReadAll();
    bool Contains(string jobId);
}
```

### `IJobTaskStorage`

Manages persistence of job task instances and their lifecycle data.

```csharp
public interface IJobTaskStorage
{
    void Add(IJobTask jobTask);
    IJobTask? FindById(string jobTaskId);
    bool Remove(string jobTaskId);
    IReadOnlyCollection<IJobTask> ReadAll();
    IReadOnlyCollection<IJobTask> ReadByJob(string jobId);
}
```

### `IJobTaskJournalStorage`

Handles persistence of job task journal entries.

```csharp
public interface IJobTaskJournalStorage
{
    void Append(string jobTaskId, JobTaskJournalEntry entry);
    IReadOnlyCollection<JobTaskJournalEntry> GetEntries(string jobTaskId);
    void Clear(string jobTaskId);
}
```

### `IExtensionOptionStorage`

Generic storage for feature-specific data, allowing extensions to persist configuration without modifying the core storage interfaces.

```csharp
public interface IExtensionOptionStorage
{
    void Set(string key, object? value);
    object? Get(string key);
    bool TryGet(string key, out object? value);
    bool Contains(string key);
    bool Remove(string key);
}
```

---



### `IExecutionMiddleware`

Pipeline component invoked around each job task execution:

```csharp
public class LoggingMiddleware : IExecutionMiddleware
{
    public async Task InvokeAsync(IJobTaskContextBuilder context, Func<Task> next, CancellationToken ct)
    {
        Console.WriteLine($"Before: {context.Job.Name}");
        await next();
        Console.WriteLine($"After: {context.JobTask.Status}");
    }
}
```

Register via `configure.AddMiddleware<LoggingMiddleware>()`.

---

## Execution Conditions

### `IExecutionCondition`

Evaluated before a job runs. Set `context.SetNotReady(reason)` to block execution:

```csharp
public class BusinessHoursCondition : IExecutionCondition
{
    public Task EvaluateAsync(ExecutionConditionContext context, CancellationToken ct)
    {
        if (DateTime.Now.Hour < 8 || DateTime.Now.Hour > 18)
            context.SetNotReady("Outside business hours");

        return Task.CompletedTask;
    }
}
```

Register via `configure.AddExecutionCondition<BusinessHoursCondition>()`.

### `IExecutionConditionController`

Coordinates multiple `IExecutionCondition` instances and produces a combined `JobConditionStatus`. The default implementation (`JobConditionController`) stops on the first not-ready result. Override via `configure.AddExecutionConditionController<T>()`.

---

## Job Task Journal

Each `IJobTask` exposes a `Journal` of `JobTaskJournalEntry` records. When a per-job logger is configured via `.WithLogger(...)`, journal entries are forwarded to it through `JobTaskJournalLoggerAdapter`.
