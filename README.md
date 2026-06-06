# ToolWheel.Extensions.JobManager

A comprehensive, extensible job management framework for .NET 8 applications. Provides fluent APIs for registering, configuring, and executing background jobs with built-in support for execution conditions, middleware pipelines, job statistics, and feature-based extensibility.

## 🎯 Overview

ToolWheel.Extensions.JobManager enables you to:

- ✅ **Register and manage jobs** with a fluent, type-safe API
- ✅ **Execute jobs** with full lifecycle management and cancellation support
- ✅ **Control execution** via composable conditions (business hours, availability checks, etc.)
- ✅ **Monitor & audit** with middleware pipelines and execution statistics
- ✅ **Extend easily** through features and auto-discovery configuration
- ✅ **Persist data** with pluggable storage implementations
- ✅ **Track execution** with rich journal entries and task status information

---

## 📦 Project Structure

This repository contains three interconnected projects:

### 1. **ToolWheel.Extensions.JobManager.Abstractions** 📋
Public API, contracts and DTOs for the Job Manager framework.

**Package:** \ToolWheel.Extensions.JobManager.Abstractions\  
**Target:** \.NET 8.0\  
**Dependencies:** \Microsoft.Extensions.DependencyInjection.Abstractions\, \Microsoft.Extensions.Logging.Abstractions\

**Contains:**
- Core interfaces: \IJob\, \IJobService\, \IJobTask\, \IJobTaskService\, etc.
- Configuration builders: \IJobDescriptionBuilder\, \IJobManagerConfigurationBuilder\
- Execution abstractions: \IExecutionMiddleware\, \IExecutionCondition\, \IExecutionConditionController\
- Storage contracts: \IJobStorage\, \IJobTaskStorage\, \IJobTaskJournalStorage\
- Extension points: \IJobManagerFeature\, \IAutoFeatureConfigurator\

👉 **[Read the Abstractions README →](Source/ToolWheel.Extensions.JobManager.Abstractions/src/README.md)**

---

### 2. **ToolWheel.Extensions.JobManager** ⚙️
Core runtime library with default implementations and DI integration.

**Package:** \ToolWheel.Extensions.JobManager\  
**Target:** \.NET 8.0\  
**Dependencies:** \ToolWheel.Extensions.JobManager.Abstractions\, \Microsoft.Extensions.DependencyInjection\

**Contains:**
- Default service implementations: \JobService\, \JobTaskService\, \JobTaskExecutionService\, etc.
- In-memory storage implementations: \InMemoryJobStorage\, \InMemoryJobTaskStorage\, etc.
- Built-in middleware: \JobTaskTargetObjectMiddleware\, \JobExecutionStatisticsMiddleware\
- Built-in conditions: \JobEnabledCondition\, \JobConditionController\
- DI extension: \ServiceCollectionExtensions.AddJobManager()\
- Feature auto-discovery system

👉 **[Read the Implementation README →](Source/ToolWheel.Extensions.JobManager/src/README.md)**

---

### 3. **ToolWheel.Extensions.JobManager.Test Projects** 🧪

Comprehensive unit test coverage for both packages:

- \ToolWheel.Extensions.JobManager.Abstractions.Test\ – 15 tests for builders and configuration
- \ToolWheel.Extensions.JobManager.Test\ – 113 tests for all services, middleware, and conditions

**Test Tools:** NUnit, Assert.That style, Moq

---

## 🚀 Quick Start

### 1. Install NuGet Packages

\\\bash
dotnet add package ToolWheel.Extensions.JobManager
\\\

This pulls in \ToolWheel.Extensions.JobManager.Abstractions\ automatically.

### 2. Register and Configure

\\\csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddJobManager(configure =>
{
    configure.ConfigureJobs(jobs =>
    {
        var worker = new ReportWorker();
        jobs.Add(\"daily-report\", worker.Generate)
            .Id(\"daily-report\")
            .Name(\"Daily Report\")
            .Description(\"Generates daily sales report\")
            .Enabled();
    });

    configure.AddExecutionCondition<BusinessHoursCondition>();
    configure.AddMiddleware<AuditMiddleware>();
    configure.ConfigureServices(svc =>
        svc.AddSingleton<IAuditLog, ConsoleAuditLog>());
});

var app = builder.Build();
\\\

### 3. Execute a Job

\\\csharp
var jobService = app.Services.GetRequiredService<IJobService>();
var job = jobService.ReadById(\"daily-report\");

var task = await jobService.ExecuteAsync(job);
Console.WriteLine(\$\"Job completed with status: {task.Status}\");

// Access journal entries
foreach (var entry in task.Journal)
    Console.WriteLine(\$\"[{entry.Level}] {entry.Message}\");
\\\

### 4. Add a Job Dynamically at Runtime

\\\csharp
jobService.Add(worker.Process, builder =>
{
    builder
        .Id(\"runtime-job\")
        .Name(\"Runtime Job\")
        .Enabled()
        .WithFeature<MyFeature>(f => f.Setting = true);
});
\\\

---

## 🔧 Key Concepts

### Jobs & Tasks

- **\IJob\** – Describes a registered background job (ID, method, metadata).
- **\IJobTask\** – Runtime handle for a single job execution (status, journal, result).

### Configuration API

- **\IJobDescriptionBuilder\** – Fluent builder to configure individual jobs.
- **\IJobManagerConfigurationBuilder\** – Top-level builder for DI setup.

### Execution Pipeline

1. **Condition Evaluation** – All \IExecutionCondition\ instances are evaluated.
2. **Middleware Pipeline** – Each \IExecutionMiddleware\ wraps the execution.
3. **Target Invocation** – The job's target method is invoked.
4. **Journal Recording** – All log entries are captured.
5. **Statistics** – Execution metrics are recorded.

### Extensibility

- **\IJobManagerFeature\** – Attach custom per-job data and behavior.
- **\IAutoFeatureConfigurator\** – Auto-discovered at startup to register services.
- **Custom Storage** – Implement \IJobStorage\, \IJobTaskStorage\, etc., to persist data.
- **Custom Middleware** – Implement \IExecutionMiddleware\ to wrap execution.
- **Custom Conditions** – Implement \IExecutionCondition\ to control readiness.

---

## 📚 Documentation

For detailed documentation, API reference, and usage examples:

- **Abstractions Package** → [README](Source/ToolWheel.Extensions.JobManager.Abstractions/src/README.md)
  - Core interfaces and contracts
  - Extension points
  - Configuration builders

- **Implementation Package** → [README](Source/ToolWheel.Extensions.JobManager/src/README.md)
  - Service implementations
  - Storage implementations
  - Middleware and conditions
  - DI registration

---

## 🧪 Testing

Both packages include comprehensive test coverage:

\\\ash
# Run all tests
dotnet test

# Run specific project tests
dotnet test Source/ToolWheel.Extensions.JobManager.Abstractions/test/
dotnet test Source/ToolWheel.Extensions.JobManager/test/
\\\

---

## 🏗️ Architecture

\\\
┌─────────────────────────────────────────────────────────┐
│  Consumer Application (uses Abstractions)               │
├─────────────────────────────────────────────────────────┤
│  ToolWheel.Extensions.JobManager (implementations)      │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Services Layer                                   │  │
│  │ - JobService, JobTaskService,                   │  │
│  │   JobTaskExecutionService, etc.                 │  │
│  └──────────────────────────────────────────────────┘  │
│                          ↓                              │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Middleware Pipeline                            │  │
│  │ - JobTaskTargetObjectMiddleware                 │  │
│  │ - Custom middleware from extensions             │  │
│  └──────────────────────────────────────────────────┘  │
│                          ↓                              │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Condition Evaluation                            │  │
│  │ - Built-in conditions                           │  │
│  │ - Custom conditions from extensions             │  │
│  └──────────────────────────────────────────────────┘  │
│                          ↓                              │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Storage Layer (pluggable)                       │  │
│  │ - InMemory implementations (default)            │  │
│  │ - Custom database implementations               │  │
│  └──────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────┤
│  ToolWheel.Extensions.JobManager.Abstractions          │
│  (interfaces, contracts, extension points)              │
└─────────────────────────────────────────────────────────┘
\\\

---

## 📋 Features at a Glance

| Feature | Details |
|---------|---------|
| **Job Registration** | Fluent API with automatic ID generation |
| **Execution Conditions** | Composable conditions to control job readiness |
| **Middleware Pipeline** | Wraps execution for logging, metrics, security, etc. |
| **Singleton Instances** | Optional reuse of target object across executions |
| **Job Statistics** | Execution count, success rate, duration metrics |
| **Task Journal** | Rich log entries with levels (Info, Warning, Error, etc.) |
| **Cancellation** | Full \CancellationToken\ support with graceful shutdown |
| **Per-Job Loggers** | Optional custom logger per job |
| **Feature System** | Attach custom data per job via \IJobManagerFeature\ |
| **Auto-Discovery** | Auto-find and configure extensions via \IAutoFeatureConfigurator\ |
| **Pluggable Storage** | Replace in-memory stores with custom persistence |

---

## 🔌 Common Extensibility Patterns

### Add a Custom Condition

\\\csharp
public class BusinessHoursCondition : IExecutionCondition
{
    public Task EvaluateAsync(ExecutionConditionContext context, CancellationToken ct)
    {
        var hour = DateTime.Now.Hour;
        if (hour < 8 || hour > 18)
            context.SetNotReady(\"Outside business hours\");
        return Task.CompletedTask;
    }
}

// Register
configure.AddExecutionCondition<BusinessHoursCondition>();
\\\

### Add Custom Middleware

\\\csharp
public class TimingMiddleware : IExecutionMiddleware
{
    public async Task InvokeAsync(IJobTaskContextBuilder context, Func<Task> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next();
        }
        finally
        {
            sw.Stop();
            Console.WriteLine(\$\"Job {context.Job.Name} took {sw.ElapsedMilliseconds}ms\");
        }
    }
}

// Register
configure.AddMiddleware<TimingMiddleware>();
\\\

### Create a Feature for Job Metadata

\\\csharp
public class MetadataFeature : IJobManagerFeature
{
    public Dictionary<string, object?> Tags { get; set; } = new();

    public void Apply(IServiceProvider serviceProvider, IJobDescription jobDescription, IJob job)
    {
        var service = serviceProvider.GetRequiredService<IMetadataService>();
        service.Register(job.Id, Tags);
    }
}

// Usage
jobs.Add(\"my-job\", worker.Run)
    .WithFeature<MetadataFeature>(f =>
    {
        f.Tags[\"department\"] = \"sales\";
        f.Tags[\"priority\"] = \"high\";
    });
\\\

---

## 📝 License

ToolWheel.Extensions.JobManager is licensed under the [LICENSE](LICENSE) included in this repository.

---

## 🤝 Contributing

Contributions are welcome! Please ensure:

- ✅ All tests pass (\dotnet test\)
- ✅ New features include unit tests
- ✅ Code follows the existing style (NUnit, Assert.That)
- ✅ XML documentation for public APIs

---

## 📞 Support

For issues, questions, or suggestions, please open an issue on [GitHub](https://github.com/JobManagerFramework/JobManager).

---

## Version

| Component | Version | Target |
|-----------|---------|--------|
| ToolWheel.Extensions.JobManager | Latest | .NET 8.0 |
| ToolWheel.Extensions.JobManager.Abstractions | Latest | .NET 8.0 |

---

*Built with ❤️ for .NET 8 applications*
