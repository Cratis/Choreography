// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Orleans.Chronicle.StateMachines;
using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Journal;
using Cratis.Orleans.Workflows.Runs;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Chronicle.Workflows;

/// <summary>
/// Represents an <see cref="IWorkflowRunJournal"/> that keeps a run's facts as events in Chronicle's event log.
/// </summary>
/// <remarks>
/// The run is the event source: its facts are appended with the run's identity as event source id, in the namespace its
/// scope maps to, and read back by event source id and fact type. An append names the sequence number of the last fact the
/// decision was made from as a concurrency scope, so Chronicle refuses it when another append got there first.
/// </remarks>
/// <param name="eventStores">The <see cref="IStateMachineEventStores"/> event stores are resolved through.</param>
/// <param name="options">The <see cref="EventSourcedWorkflowsOptions"/>.</param>
public class ChronicleWorkflowRunJournal(IStateMachineEventStores eventStores, IOptions<EventSourcedWorkflowsOptions> options) : IWorkflowRunJournal
{
    /// <inheritdoc/>
    public async Task<WorkflowRunJournalSnapshot> Load(WorkflowRunKey key)
    {
        var eventStore = await EventStoreFor(key);
        var eventTypes = WorkflowRunFacts.Types.Select(eventStore.EventTypes.GetEventTypeFor).ToList();

        IReadOnlyList<AppendedEvent> history;
        try
        {
            history = [.. await eventStore.EventLog.GetForEventSourceIdAndEventTypes(EventSourceIdOf(key), eventTypes)];
        }
        catch (ArgumentNullException)
        {
            // The client's own projection of an empty answer: a run that has no facts yet.
            history = [];
        }

        return history.Count == 0
            ? WorkflowRunJournalSnapshot.Empty
            : new([.. history.Select(appended => new WorkflowRecordedFact(appended.Content, appended.Context.Occurred))], PositionOf(history[^1].Context.SequenceNumber));
    }

    /// <inheritdoc/>
    public async Task<WorkflowJournalAppendResult> Append(WorkflowRunKey key, WorkflowJournalPosition expectedTail, IReadOnlyList<object> facts)
    {
        var eventStore = await EventStoreFor(key);
        var eventSourceId = EventSourceIdOf(key);
        var appended = await eventStore.EventLog.AppendMany(eventSourceId, facts, concurrencyScope: new ConcurrencyScope(SequenceNumberOf(expectedTail), eventSourceId));
        if (appended.IsSuccess)
        {
            return WorkflowJournalAppendResult.Appended(PositionOf(appended.TailSequenceNumber));
        }

        return appended.HasConcurrencyViolations
            ? WorkflowJournalAppendResult.Conflict(expectedTail)
            : WorkflowJournalAppendResult.Failed(
                expectedTail,
                string.Join(
                    "; ",
                    appended.Errors.Select(error => error.ToString())
                        .Concat(appended.ConstraintViolations.Select(violation => violation.Message.Value))));
    }

    static EventSourceId EventSourceIdOf(WorkflowRunKey key) => new(key.Run.Value);

    static WorkflowJournalPosition PositionOf(EventSequenceNumber number) =>
        number == EventSequenceNumber.BeforeFirst ? WorkflowJournalPosition.BeforeFirst : new((long)number.Value);

    static EventSequenceNumber SequenceNumberOf(WorkflowJournalPosition position) =>
        position == WorkflowJournalPosition.BeforeFirst ? EventSequenceNumber.BeforeFirst : new((ulong)position.Value);

    Task<IEventStore> EventStoreFor(WorkflowRunKey key) => eventStores.Get(options.Value.EventStore, options.Value.NamespaceFor(key.Scope));
}
