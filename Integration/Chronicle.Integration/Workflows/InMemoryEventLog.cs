// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Orleans.Chronicle.StateMachines;

namespace Cratis.Orleans.Chronicle.Integration.Workflows;

/// <summary>
/// Stands in for Chronicle's event log - and for Chronicle surviving a silo - behind <see cref="IStateMachineEventStores"/>.
/// Both silos of the cluster live in one process, so they share it the way two silos share a real event store.
/// </summary>
/// <remarks>
/// It keeps events in one sequence and refuses an append whose concurrency scope is not the last sequence number of its event
/// source, as Chronicle does.
/// </remarks>
public class InMemoryEventLog : IStateMachineEventStores
{
    readonly object _lock = new();
    readonly List<AppendedEvent> _events = [];
    readonly IEventStore _eventStore;

    public InMemoryEventLog()
    {
        var log = Substitute.For<IEventLog>();
        log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>()).Returns(call => Task.FromResult(For(call.Arg<EventSourceId>())));
        log.AppendMany(
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<object>>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<Cratis.Execution.CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<ConcurrencyScope?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject?>()).Returns(call => Task.FromResult(Append(call.Arg<EventSourceId>(), call.Arg<IEnumerable<object>>(), call.Arg<ConcurrencyScope?>())));
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(log);
    }

    public static InMemoryEventLog Instance { get; } = new();

    public IReadOnlyList<object> FactsOf(string eventSourceId) => [.. For(new EventSourceId(eventSourceId)).Select(appended => appended.Content)];

    public Task<IEventStore> Get(EventStoreName eventStore, EventStoreNamespaceName @namespace) => Task.FromResult(_eventStore);

    IImmutableList<AppendedEvent> For(EventSourceId eventSourceId)
    {
        lock (_lock)
        {
            return [.. _events.Where(appended => appended.Context.EventSourceId == eventSourceId)];
        }
    }

    AppendManyResult Append(EventSourceId eventSourceId, IEnumerable<object> events, ConcurrencyScope? scope)
    {
        lock (_lock)
        {
            var last = _events.LastOrDefault(appended => appended.Context.EventSourceId == eventSourceId)?.Context.SequenceNumber ?? EventSequenceNumber.BeforeFirst;
            if (scope is not null && scope.SequenceNumber != last)
            {
                return new AppendManyResult { ConcurrencyViolations = [new ConcurrencyViolation(eventSourceId, scope.SequenceNumber, last)] };
            }

            var numbers = new List<EventSequenceNumber>();
            foreach (var @event in events)
            {
                var number = new EventSequenceNumber((ulong)_events.Count);
                _events.Add(new AppendedEvent(EventContext.Empty with { EventSourceId = eventSourceId, SequenceNumber = number, Occurred = DateTimeOffset.UtcNow }, @event));
                numbers.Add(number);
            }

            return new AppendManyResult { SequenceNumbers = numbers };
        }
    }
}
