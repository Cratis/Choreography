// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationHistoryStorage;

public class when_appending_entries : given.an_operation_history_storage
{
    OperationEntry _first;
    OperationEntry _second;

    async Task Because()
    {
        _first = await Storage.Append(Entry("scope", "alice"));
        _second = await Storage.Append(Entry("scope", "alice"));
    }

    [Fact] void should_assign_positions_in_append_order() => (_second.Position > _first.Position).ShouldBeTrue();
    [Fact] void should_assign_a_position() => (_first.Position > 0).ShouldBeTrue();
}
