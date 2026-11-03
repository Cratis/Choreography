// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationHistory;

public class when_the_work_throws : given.an_operation_history
{
    Exception _exception;
    OperationEntry _entry;

    async Task Because()
    {
        _exception = await Catch.Exception(() => History.Record("scope", "alice", "kind", async recording =>
        {
            await recording.Associate([7UL]);
            throw new InvalidOperationException("boom");
        }));
        _entry = (await History.GetLatest("scope", "alice"))!;
    }

    [Fact] void should_rethrow() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_record_the_operation_as_failed() => _entry.Status.ShouldEqual(OperationStatus.Failed);
    [Fact] void should_keep_the_failure_reason() => _entry.Failure.ShouldEqual("boom");
    [Fact] void should_keep_event_references_made_before_the_failure() => _entry.EventSequenceNumbers.AsEnumerable().ShouldEqual([7UL]);
}
