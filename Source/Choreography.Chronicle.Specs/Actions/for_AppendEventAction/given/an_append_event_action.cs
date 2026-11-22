// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Chronicle.Actions.for_AppendEventAction.given;

public abstract class an_append_event_action : Specification
{
    protected record SomethingHappened(string What);

    protected IEventStore _store;
    protected IStateMachineEventStores _eventStores;
    protected IServiceProvider _services;
    protected EventStoreName _requestedStore;
    protected EventStoreNamespaceName _requestedNamespace;
    protected EventSourceId _appendedTo;
    protected object? _appended;

    void Establish()
    {
        _store = Substitute.For<IEventStore>();
        _eventStores = Substitute.For<IStateMachineEventStores>();
        _eventStores.Get(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call =>
        {
            _requestedStore = call.Arg<EventStoreName>();
            _requestedNamespace = call.Arg<EventStoreNamespaceName>();
            return Task.FromResult(_store);
        });

        _services = new ServiceCollection().AddSingleton(_eventStores).BuildServiceProvider();
    }

    protected void AppendSucceeds() => _store.EventLog
        .Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<Cratis.Execution.CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Cratis.Chronicle.Subject?>())
        .Returns(call =>
        {
            _appendedTo = call.Arg<EventSourceId>();
            _appended = call.Arg<object>();
            return Task.FromResult(new AppendResult { SequenceNumber = new EventSequenceNumber(1) });
        });
}
