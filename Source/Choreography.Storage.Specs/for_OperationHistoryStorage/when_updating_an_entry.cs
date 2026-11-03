// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationHistoryStorage;

public class when_updating_an_entry : given.an_operation_history_storage
{
    OperationEntry _original;
    OperationEntry _updated;

    async Task Because()
    {
        _original = await Storage.Append(Entry("scope", "alice"));
        _updated = await Storage.Update(_original.Id, _ => _ with { Status = OperationStatus.Completed, Position = 999, Actor = "mallory" });
    }

    [Fact] void should_change_the_lifecycle() => _updated.Status.ShouldEqual(OperationStatus.Completed);
    [Fact] void should_keep_the_position() => _updated.Position.ShouldEqual(_original.Position);
    [Fact] void should_keep_the_actor() => _updated.Actor.ShouldEqual(_original.Actor);
}
