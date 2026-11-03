// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationHistory;

public class when_getting_the_latest_operation : given.an_operation_history
{
    OperationEntry _aliceLatest;
    OperationEntry _latestInOtherScope;
    OperationEntry _nobody;
    IReadOnlyList<OperationEntry> _history;

    async Task Because()
    {
        await History.Record("scope", "alice", "first", _ => Task.CompletedTask);
        await History.Record("scope", "bob", "bobs", _ => Task.CompletedTask);
        _aliceLatest = await History.Record("scope", "alice", "second", _ => Task.CompletedTask);
        await History.Record("scope", "bob", "bobs-again", _ => Task.CompletedTask);
        await History.Record("other", "alice", "elsewhere", _ => Task.CompletedTask);
        _latestInOtherScope = (await History.GetLatest("other", "alice"))!;
        _nobody = await History.GetLatest("scope", "carol");
        _history = await History.GetFor("scope", "alice");
    }

    [Fact] async Task should_identify_the_actors_latest_operation_in_the_scope() => (await History.GetLatest("scope", "alice")).ShouldEqual(_aliceLatest);
    [Fact] void should_not_mix_scopes() => _latestInOtherScope.Kind.Value.ShouldEqual("elsewhere");
    [Fact] void should_have_none_for_an_actor_without_operations() => _nobody.ShouldBeNull();
    [Fact] void should_order_the_actors_history() => _history.Select(_ => _.Kind.Value).ShouldEqual(["first", "second"]);
}
