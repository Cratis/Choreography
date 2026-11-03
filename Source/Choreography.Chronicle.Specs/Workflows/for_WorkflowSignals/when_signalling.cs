// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Choreography.Chronicle.Workflows.Listing;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Waits;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowSignals;

public class when_signalling : Specification
{
    IWorkflowRun _matching = null!;
    IWorkflowRun _other = null!;
    int _resolved;

    async Task Because()
    {
        var contract = new WorkflowContractReference("contract-1", 1);
        var eventStore = Substitute.For<IEventStore>();
        var readModels = Substitute.For<IReadModels>();
        readModels.GetInstances<OpenWorkflowWait>(Arg.Any<EventCount>()).Returns(Task.FromResult<IEnumerable<OpenWorkflowWait>>(
        [
            Wait("wait-1", "run-1", WaitKind.Signal, "correlation-1", contract),
            Wait("wait-2", "run-2", WaitKind.Signal, "correlation-2", contract),
            Wait("wait-3", "run-3", WaitKind.Timer, "correlation-1", contract),
            Wait("wait-4", "run-4", WaitKind.Signal, "correlation-1", new WorkflowContractReference("another", 1))
        ]));
        eventStore.ReadModels.Returns(readModels);
        var eventStores = Substitute.For<IStateMachineEventStores>();
        eventStores.Get(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(Task.FromResult(eventStore));
        _matching = Substitute.For<IWorkflowRun>();
        _other = Substitute.For<IWorkflowRun>();
        var workflows = Substitute.For<IWorkflows>();
        workflows.GetRun(Arg.Any<WorkflowScope>(), new WorkflowRunId("run-1")).Returns(_matching);
        workflows.GetRun(Arg.Any<WorkflowScope>(), Arg.Is<WorkflowRunId>(run => run.Value != "run-1")).Returns(_other);
        _resolved = await new WorkflowSignals(eventStores, workflows, Options.Create(new EventSourcedWorkflowsOptions())).Signal(new WorkflowScope("scope-1"), "correlation-1", contract, "{\"ok\":true}", "someone");
    }

    [Fact] void should_resolve_the_signal_waits_with_the_correlation_and_the_contract() => _matching.Received(1).ResolveWait("wait-1", WellKnownOutlets.Resumed, "{\"ok\":true}", "someone");
    [Fact] void should_resolve_nothing_else() => _other.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_say_how_many_it_resolved() => _resolved.ShouldEqual(1);

    static OpenWorkflowWait Wait(string id, string run, WaitKind kind, string correlation, WorkflowContractReference signal) =>
        new(id, run, NodeExecutionKey.RootOf("wait"), kind, correlation, signal, WaitDeadline.None);
}
