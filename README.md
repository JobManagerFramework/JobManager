# ToolWheel JobManager

Modular infrastructure for defining, scheduling, executing and safeguarding background jobs in .NET 8 and .NET 10 applications.

## What can you do with it?

- **Register jobs** – via delegate, lambda expression or a startup class
- **Execute jobs manually** – via `IJobService.ExecuteAsync` or through the REST API

## Packages

| Package | Description |
|---|---|
| [`ToolWheel.Extensions.JobManager.Abstractions`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager.Abstractions/src/) | Public API – interfaces, DTOs and configuration contracts. No implementations. |
| [`ToolWheel.Extensions.JobManager`](Source/Extensions/JobManager/ToolWheel.Extensions.JobManager/src/) | Core runtime – `JobService`, `JobTaskService`, middleware pipeline, DI registration via `AddJobManager()`. |

## Quick Start

### Register and run a job

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddJobManager(configure =>
{
    configure.ConfigureJobs(jobs =>
    {
        jobs.Add("my-job", new MyWorker().DoWork)
            .Name("My Job")
            .Enabled();
    });
});

await builder.Build().RunAsync();
```

## Detailed Documentation

Each package has its own `README.md` in its project directory with full API reference, configuration examples and internals description.

## License

[MIT](LICENSE)