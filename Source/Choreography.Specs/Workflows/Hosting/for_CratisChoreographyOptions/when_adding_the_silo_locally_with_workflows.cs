// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage;
using Cratis.Choreography.Workflows.Journal;
using Cratis.Choreography.Workflows.Sweeping;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Storage;

namespace Cratis.Choreography.Hosting.for_CratisChoreographyOptions;

public class when_adding_the_silo_locally_with_workflows : Specification
{
    WebApplicationBuilder _builder = null!;
    ServiceProvider _provider = null!;

    void Because()
    {
        _builder = WebApplication.CreateBuilder();
        _builder.AddCratisChoreography(new CratisChoreographyOptions { Clustering = ClusteringMode.Localhost, Storage = GrainStorageMode.InMemory, Workflows = true });
        _provider = _builder.Services.BuildServiceProvider();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_not_configure_a_mongodb_client() => _builder.Services.Any(descriptor => descriptor.ServiceType.FullName?.Contains("MongoDB", StringComparison.Ordinal) ?? false).ShouldBeFalse();
    [Fact] void should_host_the_workflow_journal() => _builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IWorkflowRunJournal)).ShouldBeTrue();
    [Fact] void should_host_the_sweep() => _builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(WorkflowRunSweepService)).ShouldBeTrue();
    [Fact] void should_keep_workflow_runs_in_memory_storage() => _provider.GetKeyedService<IGrainStorage>(WellKnownGrainStorageProviders.Workflows).ShouldNotBeNull();
    [Fact] void should_keep_jobs_in_memory() => _builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IJobsStorage)).ShouldBeTrue();
}
