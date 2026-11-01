// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.Hosting;
using Cratis.Orleans.Chronicle.StateMachines;
using Cratis.Orleans.Hosting;
using Cratis.Orleans.Workflows.Journal;
using Cratis.Orleans.Workflows.Scopes;
using Cratis.Orleans.Workflows.Sweeping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Orleans.Chronicle.Workflows.for_EventSourcedWorkflowsServiceCollectionExtensions;

public class when_adding_event_sourced_workflows : Specification
{
    ServiceProvider _provider = null!;

    void Because()
    {
        var services = new ServiceCollection().AddLogging().AddSingleton(Substitute.For<IGrainFactory>());
        services.AddCratisOrleansWorkflows();
        services.AddEventSourcedWorkflows(options =>
        {
            options.EventStore = "store-1";
            options.ReadModels = false;
        });
        services.AddSingleton(Substitute.For<IStateMachineEventStores>());
        _provider = services.BuildServiceProvider();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_keep_the_facts_in_chronicle_even_when_the_core_was_added_first() => _provider.GetRequiredService<IWorkflowRunJournal>().ShouldBeOfExactType<ChronicleWorkflowRunJournal>();
    [Fact] void should_map_scopes_onto_event_store_namespaces() => _provider.GetRequiredService<IWorkflowExecutionScopes>().ShouldBeOfExactType<ChronicleWorkflowExecutionScopes>();
    [Fact] void should_signal_through_the_open_waits() => _provider.GetRequiredService<IWorkflowSignals>().ShouldBeOfExactType<WorkflowSignals>();
    [Fact] void should_run_the_schedules_in_the_background() => _provider.GetServices<IHostedService>().OfType<WorkflowScheduleService>().Count().ShouldEqual(1);
    [Fact] void should_still_sweep_in_the_background() => _provider.GetServices<IHostedService>().OfType<WorkflowRunSweepService>().Count().ShouldEqual(1);
}
