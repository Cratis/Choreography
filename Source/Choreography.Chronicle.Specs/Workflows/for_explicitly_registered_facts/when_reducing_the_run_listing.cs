// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Workflows.Listing;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Triggers;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Choreography.Chronicle.Workflows.for_explicitly_registered_facts;

/// <summary>
/// Runs the run listing reducer through the client's own reducer invoker, which finds the events it handles through the
/// event types the facts were explicitly registered as.
/// </summary>
public class when_reducing_the_run_listing : given.the_client_event_types
{
    ReducerInvoker _invoker = null!;
    ReduceResult _result = null!;

    void Because()
    {
        var activator = Substitute.For<IClientArtifactsActivator>();
        Catch<ActivatedArtifact> activated = new ActivatedArtifact(new WorkflowRunReducer(), typeof(WorkflowRunReducer), NullLogger<ActivatedArtifact>.Instance);
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(WorkflowRunReducer)).Returns(activated);
        _invoker = new ReducerInvoker(EventTypes, activator, typeof(WorkflowRunReducer), typeof(WorkflowRun), new ReadModelContainerName("workflow-runs"));

        var execution = NodeExecutionKey.RootOf("action");
        var admitted = new WorkflowRunAdmitted(
            "workflow-1",
            "area-1",
            3,
            "catalog-1",
            [],
            new WorkflowGraph([], []),
            new WorkflowTrigger(WorkflowTriggerKind.Manual, "source", "dedup", new("subject-kind", "subject-1"), WorkflowContractReference.Any, WorkflowData.Empty, [], "correlation-1", WorkflowCausationKey.None, WorkflowActor.System, DateTimeOffset.UtcNow));
        object[] facts = [admitted, new WorkflowNodeScheduled(execution, WorkflowAttempt.First, "job-1", 1), new WorkflowNodeSucceeded(execution, WorkflowAttempt.First, "next", WorkflowData.Empty, []), new WorkflowRunSucceeded()];
        var context = EventContext.Empty with { EventSourceId = new EventSourceId("run-1") };
        using var services = new ServiceCollection().BuildServiceProvider();
        _result = _invoker.Invoke(services, facts.Select((fact, index) => new EventAndContext(fact, context with { SequenceNumber = new EventSequenceNumber((ulong)index) })), null).GetAwaiter().GetResult();
    }

    [Fact] void should_handle_every_fact_a_run_listing_follows() => _invoker.EventTypes.Select(eventType => eventType.Id.Value).Order().ShouldEqual([.. WorkflowRunFacts.Types.Select(type => type.Name).Order()]);
    [Fact] void should_reduce_without_errors() => _result.ErrorMessages.ShouldBeEmpty();
    [Fact] void should_have_reduced_up_to_the_last_fact() => _result.LastSuccessfullyObservedEvent.ShouldEqual(new EventSequenceNumber(3));
    [Fact] void should_produce_the_run() => ((WorkflowRun)_result.ReadModelState!).Id.Value.ShouldEqual("run-1");
    [Fact] void should_end_with_the_status_of_the_run() => ((WorkflowRun)_result.ReadModelState!).Status.ShouldEqual(WorkflowRunStatus.Succeeded);
    [Fact] void should_track_the_execution() => ((WorkflowRun)_result.ReadModelState!).Executions.Single().Status.ShouldEqual(NodeExecutionStatus.Succeeded);
}
