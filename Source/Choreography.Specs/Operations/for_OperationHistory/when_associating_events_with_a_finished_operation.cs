// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationHistory;

public class when_associating_events_with_a_finished_operation : given.an_operation_history
{
    Exception _exception;

    async Task Because()
    {
        var entry = await History.Record("scope", "alice", "kind", _ => Task.CompletedTask);
        _exception = await Catch.Exception(() => History.AssociateEvents(entry.Id, [1UL]));
    }

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<OperationNotStarted>();
}
