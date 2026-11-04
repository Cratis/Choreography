// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationHistoryStorage;

public class when_querying_by_scope_and_actor : given.an_operation_history_storage
{
    OperationEntry _aliceFirst;
    OperationEntry _bob;
    OperationEntry _aliceSecond;
    IReadOnlyList<OperationEntry> _scopeEntries;
    IReadOnlyList<OperationEntry> _aliceEntries;

    async Task Because()
    {
        _aliceFirst = await Storage.Append(Entry("scope", "alice"));
        _bob = await Storage.Append(Entry("scope", "bob"));
        await Storage.Append(Entry("other", "alice"));
        _aliceSecond = await Storage.Append(Entry("scope", "alice"));
        _scopeEntries = await Storage.GetFor("scope");
        _aliceEntries = await Storage.GetFor("scope", "alice");
    }

    [Fact] void should_return_everything_in_the_scope_in_order() => _scopeEntries.Select(_ => _.Id).ShouldEqual([_aliceFirst.Id, _bob.Id, _aliceSecond.Id]);
    [Fact] void should_narrow_to_the_actor_in_order() => _aliceEntries.Select(_ => _.Id).ShouldEqual([_aliceFirst.Id, _aliceSecond.Id]);
}
