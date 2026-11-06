// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Jobs.for_JobsRecovery.given;
using Cratis.Choreography.Workflows.Sweeping;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Choreography.Hosting.for_CratisChoreographySiloBuilderExtensions.given;

public class a_host_with_jobs_recovery : Specification
{
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    protected static readonly JobsManagerKey HostKey = new("jobs", "ordinary");
    protected static readonly JobsManagerKey WorkflowKey = new("workflows", "scope-a");
    protected WebApplication _application;
    protected IJobsManagerKeys _hostKeys;
    protected IJobsManagerKeys[] _providers;
    protected IJobsManager _ordinaryManager;
    protected IJobsManager _workflowManager;
    protected int _workflowCatalogActivations;
    readonly TaskCompletionSource _watching = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ControlledTimeProvider _timeProvider = new();
    WebApplicationBuilder _builder;
    JobsRecovery _recovery;
    ILifecycleObserver _observer;
    IClusterMembershipService _membership;
    ILocalSiloDetails _local;
    IGrainFactory _grainFactory;

    void Establish()
    {
        _builder = WebApplication.CreateBuilder();
        _hostKeys = Substitute.For<IJobsManagerKeys>();
        _hostKeys.GetKeys(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<JobsManagerKey>>([HostKey]));
        var types = Substitute.For<ITypes>();
        types.FindMultiple<IJobsManagerKeys>().Returns([typeof(JobsManagerKeys), typeof(WorkflowJobsManagerKeys), _hostKeys.GetType()]);
        _builder.Services.AddSingleton(types);
        _builder.Services.AddSingleton(_hostKeys.GetType(), _hostKeys);
        _builder.Services.AddSingleton(services =>
        {
            _workflowCatalogActivations++;
            return ActivatorUtilities.CreateInstance<WorkflowJobsManagerKeys>(services);
        });
        _membership = Substitute.For<IClusterMembershipService>();
        _membership.MembershipUpdates.Returns(MembershipUpdates());
        _local = Substitute.For<ILocalSiloDetails>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _ordinaryManager = Substitute.For<IJobsManager>();
        _workflowManager = Substitute.For<IJobsManager>();
        _ordinaryManager.Rehydrate().Returns(Task.CompletedTask);
        _workflowManager.Rehydrate().Returns(Task.CompletedTask);
        _grainFactory.GetGrain<IJobsManager>(0, HostKey).Returns(_ordinaryManager);
        _grainFactory.GetGrain<IJobsManager>(0, WorkflowKey).Returns(_workflowManager);
    }

    protected async Task RecoverJobs(bool workflows)
    {
        _builder.AddCratisChoreography(new()
        {
            Storage = GrainStorageMode.InMemory,
            Workflows = workflows,
            ConfigureWorkflows = options => options.Scopes.Add("scope-a")
        });
        _builder.Host.UseOrleans(silo => silo.AddCratisChoreographyJobsRecovery());
        _application = _builder.Build();
        var providers = _application.Services.GetRequiredService<IInstancesOf<IJobsManagerKeys>>();
        _providers = providers.ToArray();
        _recovery = new(_membership, _local, providers, _grainFactory, NullLogger<JobsRecovery>.Instance, _timeProvider);
        var lifecycle = Substitute.For<ISiloLifecycle>();
        lifecycle.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Do<ILifecycleObserver>(observer => _observer = observer));
        _recovery.Participate(lifecycle);
        await _observer.OnStart(CancellationToken.None);
        await _watching.Task.WaitAsync(Deadline, TimeProvider.System);
    }

    async IAsyncEnumerable<ClusterMembershipSnapshot> MembershipUpdates([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _watching.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, TimeProvider.System, cancellationToken);
        yield break;
    }

    async Task Destroy()
    {
        if (_observer is not null)
        {
            await _observer.OnStop(CancellationToken.None).WaitAsync(Deadline, TimeProvider.System);
        }
        _recovery?.Dispose();
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }
    }
}
