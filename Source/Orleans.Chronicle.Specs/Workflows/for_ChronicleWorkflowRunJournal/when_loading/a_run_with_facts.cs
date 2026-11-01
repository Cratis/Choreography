// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Orleans.Workflows.Journal;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Chronicle.Workflows.for_ChronicleWorkflowRunJournal.when_loading;

public class a_run_with_facts : given.a_journal
{
    static readonly DateTimeOffset _first = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset _second = _first.AddMinutes(1);

    WorkflowRunJournalSnapshot _snapshot = null!;
    IEnumerable<EventType> _asked = [];

    void Establish() => Store.EventLog
        .GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>())
        .Returns(call =>
        {
            _asked = call.Arg<IEnumerable<EventType>>();
            return Task.FromResult<IImmutableList<AppendedEvent>>([Appended(new WorkflowRunSucceeded(), 7, _first), Appended(new WorkflowJobStopped("job-1"), 8, _second)]);
        });

    async Task Because() => _snapshot = await Journal.Load(Key);

    [Fact] void should_return_the_facts_in_order() => _snapshot.Facts.Select(fact => fact.Fact.GetType()).ShouldEqual([typeof(WorkflowRunSucceeded), typeof(WorkflowJobStopped)]);
    [Fact] void should_carry_when_each_was_recorded() => _snapshot.Facts.Select(fact => fact.Occurred).ShouldEqual([_first, _second]);
    [Fact] void should_end_at_the_last_sequence_number() => _snapshot.Tail.ShouldEqual(new WorkflowJournalPosition(8));
    [Fact] void should_ask_for_every_fact_type() => _asked.Count().ShouldEqual(WorkflowRunFacts.Types.Count);
    [Fact] void should_ask_by_the_run_as_event_source() => Store.EventLog.Received(1).GetForEventSourceIdAndEventTypes(Arg.Is<EventSourceId>(id => id.Value == "run-1"), Arg.Any<IEnumerable<EventType>>());
}
