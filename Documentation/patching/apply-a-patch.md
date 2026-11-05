---
title: Apply a patch when a host starts
description: Write an ICanApplyPatch, enable Choreography patching with in-memory storage, set the host version, and inspect logs for patches run on startup.
---

This page takes you from nothing to a host that applies two patches when it starts, and shows what the log says while it
does. It uses in-memory storage so you need no database. When you ship, swap in the storage from
[Choose patch storage](choose-patch-storage.md).

You need an ASP.NET Core project that references the `Cratis.Choreography` and `Cratis.Choreography.Storage` packages.

## Write a patch

A patch implements `ICanApplyPatch`. This one seeds a greeting; `Greetings` stands in for whatever data your release needs to
change:

```csharp
// Greetings.cs
namespace PatchingSample;

public class Greetings
{
    public List<string> Said { get; } = [];
}
```

```csharp
// SeedGreetings.cs
using Cratis.Choreography.Patching;

namespace PatchingSample;

public class SeedGreetings(Greetings greetings) : ICanApplyPatch
{
    public SemanticVersion Version => new(1, 5, 0);

    public Task Up()
    {
        if (!greetings.Said.Contains("hello"))
        {
            greetings.Said.Add("hello");
        }

        return Task.CompletedTask;
    }
}
```

```csharp
// AddFarewell.cs
using Cratis.Choreography.Patching;

namespace PatchingSample;

public class AddFarewell(Greetings greetings) : ICanApplyPatch
{
    public SemanticVersion Version => new(1, 6, 0);

    public Task Up()
    {
        if (!greetings.Said.Contains("goodbye"))
        {
            greetings.Said.Add("goodbye");
        }

        return Task.CompletedTask;
    }
}
```

The contract has four rules:

- **`Version`** is the release that introduced the change. Patches run in ascending order of it, and a patch whose version is
  not newer than the store's patched version is never applied.
- **`Name`** defaults to the type name. The record of applied patches is keyed by it, so renaming a patch makes it a
  different patch and applies it again. Override `Name` only when you must rename the class and keep its history.
- **`Up()` must be safe to run more than once.** The record is written only after `Up()` completes, so a patch that failed or
  was interrupted is attempted again. Both patches above check before they change anything.
- **Dependencies come from the container.** A patch is created with the silo's services, so it can take storage, loggers or
  your own services in its constructor. A patch you registered yourself is used as registered.

You register nothing for discovery. Any non-abstract class that implements `ICanApplyPatch` in a loaded assembly is found.

## Turn patching on

The co-hosted silo applies patches when `Patching` is on. Set the host version so the store is marked with it:

```csharp
// Program.cs
using PatchingSample;
using Cratis.Choreography.Hosting;
using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage.Patching;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChoreography(new CratisChoreographyOptions
{
    Clustering = ClusteringMode.Localhost,
    Storage = GrainStorageMode.InMemory,
    Patching = true,
    ConfigurePatching = patching => patching.HostVersion = new SemanticVersion(2, 0, 0)
});

builder.Services.AddSingleton<Greetings>();

var app = builder.Build();
await app.StartAsync();

var greetings = app.Services.GetRequiredService<Greetings>();
Console.WriteLine($"Greetings: {string.Join(", ", greetings.Said)}");

var storage = app.Services.GetRequiredService<IPatchStorage>();
Console.WriteLine($"Patched version: {await storage.GetPatchedVersion()}");
Console.WriteLine($"Host version: {await storage.GetHostVersion()}");
foreach (var patch in (await storage.GetAll()).OrderBy(_ => _.Version))
{
    Console.WriteLine($"Recorded: {patch.Name} ({patch.Version})");
}

await app.StopAsync();
```

With `Storage = GrainStorageMode.InMemory`, the host also registers in-memory patch storage. With any other storage mode you
register an `IPatchStorage` yourself, and starting without one fails.

## What the log says

Patch application logs at the `Information` level from `Cratis.Choreography.Patching.PatchManager`. Running the program prints
these lines, among the Orleans startup output:

```text
info: Cratis.Choreography.Patching.PatchManager[442072769]
      Applying patch SeedGreetings for version 1.5.0 on host 2.0.0
info: Cratis.Choreography.Patching.PatchManager[563750987]
      Successfully applied patch SeedGreetings
info: Cratis.Choreography.Patching.PatchManager[442072769]
      Applying patch AddFarewell for version 1.6.0 on host 2.0.0
info: Cratis.Choreography.Patching.PatchManager[563750987]
      Successfully applied patch AddFarewell
```

and the program's own output:

```text
Greetings: hello, goodbye
Patched version: 1.6.0
Host version: 2.0.0
Recorded: SeedGreetings (1.5.0)
Recorded: AddFarewell (1.6.0)
```

The patches ran oldest first. The store now holds both records, a patched version of `1.6.0` (the newest patch) and a host
version of `2.0.0`. A second start against the same storage applies nothing. With in-memory storage a restart forgets the
records, so there it applies both again.

Other messages help when something is unexpected. At `Debug`, `Found {Count} patches to apply` and `Patch application process
completed. Applied {AppliedCount} patches and skipped {SkippedCount} patches`. At `Warning`, `Skipping patches on host ...:
host ... has already patched this store` and `Host version ... is unknown; bypassing the patch version gate without recording
a host version`. A failed patch logs `Failed to apply patch {PatchName}` at `Error` with the exception, and an exhausted
relocation logs at `Critical`.

## Run patches yourself

`AddCratisChoreographyPatching` on an `ISiloBuilder` applies patches when the silo reaches the `Active` lifecycle stage, and so
does `CratisChoreographyOptions.Patching`. If your host must patch at a precise point of its own startup, for example after it
prepared what the patches rely on, register the services without the startup task and call the runner:

```csharp
using Cratis.Choreography;
using Cratis.Choreography.Hosting;
using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCratisChoreographyInMemoryPatchStorage();
builder.Host.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.Services.AddCratisChoreographySerializers();
    silo.AddCratisChoreographyPatchingServices();
});

var app = builder.Build();
await app.StartAsync();

// Your own preparation goes here, then:
await app.Services.GetRequiredService<IPatchRunner>().ApplyPatches();
```

`ApplyPatches` returns when the patch manager has completed, and throws when a patch fails or when the manager could not be
relocated. Treat a throw as a failed start. This host has no `HostVersion`, so the log shows the unknown-version warning and
the store is not marked with a host version; pass a callback such as
`AddCratisChoreographyPatchingServices(patching => patching.HostVersion = new SemanticVersion(2, 0, 0))`.
Running it with the sample patches printed the same `Applying patch` and `Successfully applied patch` lines.

## Requirements

- A registered `IPatchStorage`. Patching throws when the silo starts without one.
- The Choreography serializers on the silo, because patch outcomes and versions cross a grain boundary as plain Cratis types.
- A `HostVersion` you control if your entry assembly's informational version is not the version you ship. See
  [Patching](index.md#unknown-host-versions) for what an unknown version changes.
