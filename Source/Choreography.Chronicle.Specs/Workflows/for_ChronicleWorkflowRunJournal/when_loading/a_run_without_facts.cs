// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Workflows.Journal;
using Cratis.Chronicle.Events;

namespace Cratis.Choreography.Chronicle.Workflows.for_ChronicleWorkflowRunJournal.when_loading;

public class a_run_without_facts : given.a_journal
{
    WorkflowRunJournalSnapshot _snapshot = null!;

    void Establish() => Store.EventLog
        .GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>())
        .Returns<IImmutableList<AppendedEvent>>(_ => throw new ArgumentNullException("events", "the client's own projection of an empty answer"));

    async Task Because() => _snapshot = await Journal.Load(Key);

    [Fact] void should_have_no_facts() => _snapshot.Facts.ShouldBeEmpty();
    [Fact] void should_end_before_the_first_position() => _snapshot.Tail.ShouldEqual(WorkflowJournalPosition.BeforeFirst);
    [Fact] void should_read_from_the_event_store_of_the_options() => RequestedStore.Value.ShouldEqual("workflows-store");
    [Fact] void should_read_from_the_namespace_of_the_scope() => RequestedNamespace.Value.ShouldEqual("ns-tenant-1");
}
