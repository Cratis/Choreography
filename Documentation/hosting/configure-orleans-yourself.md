---
title: Configure Orleans without the helper
description: Register the Choreography job system, serializers, storage providers, recovery, workflows and patching on your own Orleans silo builder.
---

Use this page when your application already builds its own Orleans silo, for example with a custom clustering provider, and you
cannot hand it over to `AddCratisChoreography`. You register the same pieces yourself. Anything you leave out is something the
job system silently does not have: jobs without their storage providers fail on first use, and a silo without the serializers
rejects Cratis types on cross-silo calls.

## Register the job system

This silo builder does what `AddCratisChoreography` does, with localhost clustering and in-memory jobs storage so it runs
anywhere. Swap the clustering and the storage line for your own:

```csharp
using Cratis.Choreography;
using Cratis.Choreography.Hosting;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Runtime.Hosting;
using Orleans.Serialization.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCratisChoreographyInMemoryJobsStorage();
builder.Services.AddCratisChoreographyInMemoryPatchStorage();

builder.Host.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "my-service";
        options.ServiceId = "my-service";
    });
    silo.Services.Configure<TypeManifestOptions>(options => options.EnableConfigurationAnalysis = false);
    silo.UseInMemoryReminderService();

    silo.Services.AddCratisChoreographySerializers();
    silo.Services.TryAddSingleton<ITypes>(_ => new Types());
    silo.Services.TryAddSingleton<IJobTypes, JobTypes>();
    silo.Services.TryAddSingleton<IJobs, Jobs>();
    silo.Services.TryAddSingleton<IJobStepThrottle, JobStepThrottle>();
    silo.Services.TryAddSingleton<IOptions<JobsOptions>>(_ => Options.Create(new JobsOptions()));

    silo.Services.AddGrainStorage<JobGrainStorageProvider>(
        WellKnownGrainStorageProviders.Jobs,
        (services, _) => new(
            services.GetRequiredService<IJobsStorage>(),
            services.GetRequiredService<ILogger<JobGrainStorageProvider>>()));
    silo.Services.AddGrainStorage<JobStepGrainStorageProvider>(
        WellKnownGrainStorageProviders.JobSteps,
        (services, _) => new(services.GetRequiredService<IJobsStorage>()));

    silo.AddCratisChoreographyJobsRecovery();
    silo.AddCratisChoreographyWorkflows();
    silo.AddMemoryGrainStorage(WellKnownGrainStorageProviders.Workflows);
    silo.AddCratisChoreographyPatching();
});

var app = builder.Build();
app.Run();
```

## What each registration does

| Registration | Why the silo needs it |
| --- | --- |
| `AddCratisChoreographySerializers()` | Codecs for concepts, OneOf, expando objects, LINQ iterators and the JSON fallback. See [Serializers and grain keys](../serializers-and-keys.md). |
| `EnableConfigurationAnalysis = false` | Skips Orleans' configuration analysis, which can fail at startup on serializable types in client-facing assemblies that this silo never serializes. Optional. |
| `ITypes`, `IJobTypes`, `IJobs`, `IJobStepThrottle`, `IOptions<JobsOptions>` | The job system's collaborators: type discovery, the job type registry, the `IJobs` entry point, the per-silo step limit and its options. |
| `AddGrainStorage` for `jobs` and `job-steps` | Job grains persist through providers named by `WellKnownGrainStorageProviders.Jobs` and `.JobSteps`. Both read the `IJobsStorage` you register. |
| `UseInMemoryReminderService()` | Never the MongoDB reminder table. See [Reminders and recovery](reminders-and-recovery.md). |
| `AddCratisChoreographyJobsRecovery()` | Resumes interrupted jobs after startup and after a member failure. |
| `AddCratisChoreographyWorkflows()` and the `workflows` provider | The workflow engine. The run grains store their facts under the grain storage provider named `workflows`, so register one. Omit both if you do not host workflows. |
| `AddCratisChoreographyPatching()` and an `IPatchStorage` | Applies patches when the silo becomes Active. `AddCratisChoreographyPatchingServices()` registers the same services without the startup task, for a host that calls `IPatchRunner` itself. |

## Choose clustering and durable storage

The sample uses localhost clustering and in-memory storage. For a durable silo, replace those lines:

- Register durable jobs storage on the service collection with `AddCratisChoreographyMongoDBJobsStorage` or
  `AddCratisChoreographySqlJobsStorage`, instead of the in-memory call. See [Storage](../storage/index.md).
- Back the `workflows` provider with a durable Orleans grain storage, for example `AddMongoDBGrainStorage` from
  `Orleans.Providers.MongoDB`, instead of `AddMemoryGrainStorage`.
- Register an `IPatchStorage` that matches your jobs storage; see [Choose patch storage](../patching/choose-patch-storage.md).
- Use your own clustering provider in place of `UseLocalhostClustering()`.

`AddCratisChoreography` also calls `AddActivityPropagation()` and registers `UseMongoDBClient` when MongoDB is involved; call
those yourself if you need them.

## Keep the registrations aligned across silos

Every silo in a cluster must register the same serializers and the same grain storage provider names, and the same placement
strategies if you use them. A silo that lacks a registration cannot place or decode what its peers send. Keep the
[Orleans package alignment](index.md#orleans-package-alignment) in mind when you pin versions.

## Next steps

- [Hosting API](../reference/hosting-api.md) lists each extension method with its prerequisites.
- [Host the Choreography silo](index.md) is the short path when the co-host helper fits.
