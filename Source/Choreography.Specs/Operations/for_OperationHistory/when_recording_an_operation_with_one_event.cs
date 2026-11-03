// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationHistory;

public class when_recording_an_operation_with_one_event : given.an_operation_history
{
    OperationEntry _entry;

    async Task Because() => _entry = await History.Record("scope", "alice", "kind", recording => recording.Associate([42UL]));

    [Fact] void should_keep_the_sequence_number_as_a_reference() => _entry.EventSequenceNumbers.AsEnumerable().ShouldEqual([42UL]);
    [Fact] async Task should_be_stored() => (await History.Get(_entry.Id)).ShouldEqual(_entry);
}
