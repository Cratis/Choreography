// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Choreography.Workflows;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows.for_ChronicleWorkflowRunJournal.given;

public class a_journal : Specification
{
    protected IEventStore Store = null!;
    protected IStateMachineEventStores EventStores = null!;
    protected ChronicleWorkflowRunJournal Journal = null!;
    protected WorkflowRunKey Key = new(new WorkflowScope("tenant-1"), new WorkflowRunId("run-1"));
    protected EventStoreNamespaceName RequestedNamespace;
    protected EventStoreName RequestedStore;

    void Establish()
    {
        Store = Substitute.For<IEventStore>();
        EventStores = Substitute.For<IStateMachineEventStores>();
        EventStores.Get(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call =>
        {
            RequestedStore = call.Arg<EventStoreName>();
            RequestedNamespace = call.Arg<EventStoreNamespaceName>();
            return Task.FromResult(Store);
        });
        Journal = new(EventStores, Options.Create(new EventSourcedWorkflowsOptions { EventStore = "workflows-store", NamespaceFor = scope => new($"ns-{scope.Value}") }));
    }

    protected static AppendedEvent Appended(object fact, ulong sequenceNumber, DateTimeOffset occurred) =>
        new(EventContext.Empty with { SequenceNumber = new EventSequenceNumber(sequenceNumber), Occurred = occurred }, fact);
}
