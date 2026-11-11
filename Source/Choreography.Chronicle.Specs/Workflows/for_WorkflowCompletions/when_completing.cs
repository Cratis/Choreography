// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Choreography.Chronicle.Workflows.Listing;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Waits;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows.for_WorkflowCompletions;

public class when_completing : Specification
{
    IWorkflowRun _matching = null!;
    IWorkflowRun _other = null!;
    int _completed;

    async Task Because()
    {
        var eventStore = Substitute.For<IEventStore>();
        var readModels = Substitute.For<IReadModels>();
        readModels.GetInstances<OpenWorkflowWait>(Arg.Any<EventCount>()).Returns(Task.FromResult<IEnumerable<OpenWorkflowWait>>(
        [
            Wait("wait-1", "run-1", WaitKind.Completion, "invocation-1"),
            Wait("wait-2", "run-2", WaitKind.Completion, "invocation-2"),
            Wait("wait-3", "run-3", WaitKind.Signal, "invocation-1")
        ]));
        eventStore.ReadModels.Returns(readModels);
        var eventStores = Substitute.For<IStateMachineEventStores>();
        eventStores.Get(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(Task.FromResult(eventStore));
        _matching = Substitute.For<IWorkflowRun>();
        _other = Substitute.For<IWorkflowRun>();
        var workflows = Substitute.For<IWorkflows>();
        workflows.GetRun(Arg.Any<WorkflowScope>(), new WorkflowRunId("run-1")).Returns(_matching);
        workflows.GetRun(Arg.Any<WorkflowScope>(), Arg.Is<WorkflowRunId>(run => run.Value != "run-1")).Returns(_other);
        _completed = await new WorkflowCompletions(eventStores, workflows, Options.Create(new EventSourcedWorkflowsOptions())).Complete(new WorkflowScope("scope-1"), "invocation-1", "answered", "{\"ok\":true}", "extension");
    }

    [Fact] void should_resolve_the_completion_wait_under_the_key() => _matching.Received(1).ResolveWait("wait-1", "answered", "{\"ok\":true}", "extension");
    [Fact] void should_resolve_nothing_else() => _other.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_say_how_many_it_completed() => _completed.ShouldEqual(1);

    static OpenWorkflowWait Wait(string id, string run, WaitKind kind, string correlation) =>
        new(id, run, NodeExecutionKey.RootOf("ask"), kind, correlation, WorkflowContractReference.Any, WaitDeadline.None);
}
