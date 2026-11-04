// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationHistory;

public class when_recording_an_operation_with_no_events : given.an_operation_history
{
    OperationEntry _entry;

    async Task Because() => _entry = await History.Record("scope", "alice", "kind", _ => Task.CompletedTask);

    [Fact] void should_be_completed() => _entry.Status.ShouldEqual(OperationStatus.Completed);
    [Fact] void should_have_no_event_references() => _entry.EventSequenceNumbers.ShouldBeEmpty();
    [Fact] void should_be_a_forward_operation() => _entry.Role.ShouldEqual(OperationRole.Forward);
}
