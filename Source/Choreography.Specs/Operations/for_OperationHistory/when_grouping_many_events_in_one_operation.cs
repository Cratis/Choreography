// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationHistory;

public class when_grouping_many_events_in_one_operation : given.an_operation_history
{
    OperationEntry _entry;
    IReadOnlyList<OperationEntry> _all;

    async Task Because()
    {
        _entry = await History.Record("scope", "alice", "kind", async recording =>
        {
            await recording.Associate([1UL, 2UL]);
            await recording.Associate([3UL]);
        });
        _all = await History.GetFor("scope");
    }

    [Fact] void should_associate_all_events_with_the_one_operation() => _entry.EventSequenceNumbers.AsEnumerable().ShouldEqual([1UL, 2UL, 3UL]);
    [Fact] void should_have_a_single_operation() => _all.Count.ShouldEqual(1);
}
