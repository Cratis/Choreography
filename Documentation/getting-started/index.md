---
title: Run your first durable job
description: Build a service that runs a multi-step job on Orleans, then stop the process mid-job and watch the job finish after the restart.
---

Provisioning a tenant calls several services and takes a minute. If a deploy restarts your process halfway, a plain
background task is gone and nobody knows which services were already called. Cratis Choreography is the runtime for
durable business processes on Microsoft Orleans. Its job system is the smallest piece: you describe the steps, it runs
them in parallel on grains, keeps their progress in storage and finishes them after a restart.

By the end of this page you have a web service that starts a two-step job, shows its progress, and resumes a job you
killed mid-flight. You need a .NET SDK; the packages target `net8.0`, `net9.0` and `net10.0`. The restart part also
needs MongoDB on `mongodb://localhost:27017`.

## Create the project

```bash
dotnet new web -n Provisioning && cd Provisioning && dotnet add package Cratis.Choreography
```

## Define a step

A job does its work in **step grains**. Each step is an Orleans grain of its own, so slow steps run side by side and a
hung step cannot block the job's bookkeeping. A step only sees its own persisted state when it performs, not the
request it was created from, so anything it needs goes into that state.

```csharp title="FeatureStep.cs"
using Cratis.Choreography;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;
using Orleans.Runtime;

namespace Provisioning;

public class FeatureStepState : JobStepState
{
    public string Feature { get; set; } = string.Empty;
}

public interface IFeatureStep : IJobStep<string, string, FeatureStepState>;

public class FeatureStep(
    [PersistentState(nameof(FeatureStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<FeatureStepState> state,
    IJobStepThrottle throttle,
    ILogger<FeatureStep> logger) : JobStep<string, string, FeatureStepState>(state, throttle, logger), IFeatureStep
{
    protected override Task<Result<PrepareJobStepError>> PrepareStep(string request) =>
        Task.FromResult(Result<PrepareJobStepError>.Success());

    protected override ValueTask InitializeState(string request)
    {
        State.Feature = request;
        return ValueTask.CompletedTask;
    }

    protected override async Task<Catch<JobStepResult>> PerformStep(FeatureStepState currentState, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); // the real work goes here
        logger.LogInformation("Provisioned {Feature}", currentState.Feature);
        return Catch<JobStepResult>.Success(JobStepResult.Succeeded(currentState.Feature));
    }

    protected override ValueTask<string?> CreateCancelledResultFromCurrentState(FeatureStepState currentState) =>
        ValueTask.FromResult<string?>(string.Empty);
}
```

`InitializeState` copies the request into the state. `PerformStep` does the work and gets a token that is cancelled
when the job stops. `CreateCancelledResultFromCurrentState` supplies the result a stopped step reports.

:::note[Register your state type with a serializer]
The state travels between grains and storage, so Orleans needs a serializer for it. Types in namespaces that start
with `Cratis` are covered; your own state types are not, so `Program.cs` below registers `FeatureStepState` with
Orleans' JSON serializer. Without it the step fails with `Could not find a copier for type`.
:::

## Define the job

The job turns one request into a list of steps. Planning is separate from performing, so the job knows how many steps
it has before any of them runs, and that is what makes progress observable.

```csharp title="ProvisionTenant.cs"
using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;

namespace Provisioning;

[GenerateSerializer]
public record ProvisionTenant([Id(0)] string TenantId, [Id(1)] IReadOnlyList<string> Features) : IJobRequest;

public interface IProvisionTenantJob : IJob<ProvisionTenant>;

public class ProvisionTenantJob : Job<ProvisionTenant, JobState>, IProvisionTenantJob
{
    protected override bool KeepAfterCompleted => true;

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ProvisionTenant request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(
            request.Features.Select(feature => CreateStep<IFeatureStep>(feature)).ToImmutableList());
}
```

The grain class must be named like its interface without the `I` (`IProvisionTenantJob` and `ProvisionTenantJob`),
because the framework finds the interface from the class name. `CreateStep<T>` builds the step with the job's own
storage scope, so use it rather than constructing a `JobStepDetails` yourself. Completed jobs remove themselves by
default; `KeepAfterCompleted` keeps this one so you can query it. Job and step types are discovered, so nothing is
registered.

## Host the silo and start the job

```csharp title="Program.cs"
using Cratis.Choreography.Hosting;
using Cratis.Choreography.Jobs;
using Orleans.Serialization;
using Provisioning;

var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChoreography(CratisChoreographyOptions.Local());
builder.Services.AddSerializer(serializer =>
    serializer.AddJsonSerializer(type => type == typeof(FeatureStepState)));

var app = builder.Build();

app.MapGet("/provision/{tenantId}", async (string tenantId, IGrainFactory grainFactory) =>
{
    var manager = grainFactory.GetJobsManager("my-service", string.Empty);
    var result = await manager.Start<IProvisionTenantJob, ProvisionTenant>(
        new ProvisionTenant(tenantId, ["billing", "reporting"]));

    return result.Match(
        jobId => Results.Accepted($"/jobs/{jobId}", new { jobId = jobId.Value }),
        error => Results.Problem(error.ToString()));
});

app.MapGet("/jobs", async (IGrainFactory grainFactory) =>
{
    var manager = grainFactory.GetJobsManager("my-service", string.Empty);
    var jobs = await manager.GetAllJobs();
    return jobs.Select(job => new { id = job.Id.Value, status = job.Status.ToString(), job.Progress.SuccessfulSteps });
});

app.Run();
```

`AddCratisChoreography` co-hosts an Orleans silo inside the web application. `CratisChoreographyOptions.Local()` keeps
everything in memory, so nothing survives the process yet. The two strings passed to `GetJobsManager` are a storage
scope and a namespace. They are opaque to the framework and decide which database holds the job;
[Storage](../storage/index.md) explains how to choose them.

## Run it

```bash
dotnet run --urls http://localhost:5000
curl -i http://localhost:5000/provision/42
curl http://localhost:5000/jobs
```

```text
HTTP/1.1 202 Accepted
Location: /jobs/ab2530c7-ec8d-4129-a04a-94a3fed50a2a

{"jobId":"ab2530c7-ec8d-4129-a04a-94a3fed50a2a"}
[{"id":"ab2530c7-ec8d-4129-a04a-94a3fed50a2a","status":"Running","successfulSteps":0}]
```

Starting a job returns as soon as the job exists, hence `202 Accepted`. Five seconds later the same query returns
`"status":"CompletedSuccessfully"` with `"successfulSteps":2`, and the console shows both steps:

```text
info: Provisioning.FeatureStep[0]
      Provisioned reporting
info: Provisioning.FeatureStep[0]
      Provisioned billing
```

The steps ran in parallel, up to `JobsOptions.MaxParallelSteps` at a time (default: one fewer than the processor count).

## Make it survive a restart

Local mode forgets everything on exit. Add the MongoDB provider:

```bash
dotnet add package Cratis.Choreography.Storage.MongoDB
```

Replace the `AddCratisChoreography` line in `Program.cs` with this, and add `using Cratis.Choreography.Setup;` and
`using MongoDB.Driver;` at the top:

```csharp
builder.AddCratisChoreography(new CratisChoreographyOptions
{
    ClusterId = "my-service",
    ServiceId = "my-service"
});
builder.Services.AddCratisChoreographyMongoDBJobsStorage(new MongoClient("mongodb://localhost:27017"));
```

Then tell recovery which scope and namespace to look in after a start:

```csharp title="ServiceJobsKeys.cs"
using Cratis.Choreography.Jobs;

namespace Provisioning;

public class ServiceJobsKeys : IJobsManagerKeys
{
    public Task<IEnumerable<JobsManagerKey>> GetKeys(CancellationToken cancellationToken) =>
        Task.FromResult<IEnumerable<JobsManagerKey>>([new JobsManagerKey("my-service", string.Empty)]);
}
```

Start the service, call `/provision/42`, and stop the process with `kill -9` while the steps are running. Start it
again and query `/jobs` once a second. The job is `Running` right after the start, and about five seconds later it
turns `CompletedSuccessfully`. The second run's log shows `Provisioned billing` and `Provisioned reporting`, and the
first run's log shows neither, so the steps ran again from their persisted state in the new process.

:::caution[Without a keys provider the job stays Running]
After a restart a silo only knows the scopes it has served since it started, and a fresh process has served none. A
host that omits `IJobsManagerKeys` finds the job in storage, still `Running`, and nothing resumes it. The class is
found by convention; list every scope and namespace your service uses.
:::

:::note[Startup errors from the workflow key provider]
A silo without workflows logs `Failed to resolve or enumerate jobs manager keys from
...WorkflowJobsManagerKeys` while recovery starts. Recovery of your own keys still completes.
:::

## What the framework did for you

- It co-hosted an Orleans silo in your web process and found the job and step grains by convention.
- It kept the job, its plan and each step's state in MongoDB, which the restarted process read back.
- It restarted a step that was running when the process died, so make steps safe to repeat.

## Where to go next

- [Choosing a building block](../choosing-a-building-block.md) compares jobs with workflows, state machines and workers.
- [Run your first workflow](first-workflow.md) models a process as a graph of nodes.
- [Jobs](../jobs/index.md) covers planning, stages, retries, stopping and recovery.
- [Hosting](../hosting/index.md) covers clustering and the silo options.
