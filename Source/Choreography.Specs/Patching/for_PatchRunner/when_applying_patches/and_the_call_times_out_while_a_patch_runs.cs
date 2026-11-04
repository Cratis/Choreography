// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_the_call_times_out_while_a_patch_runs : given.a_patch_runner
{
    int _calls;
    Exception _failure;

    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => ++_calls < 4
        ? Task.FromException<PatchOutcome>(new TimeoutException(PatchRunner.ResponseTimeoutPrefix + " in 00:00:30 for message: Request"))
        : Task.FromResult(PatchOutcome.Completed));

    async Task Because() => _failure = await Cratis.Specifications.Catch.Exception(Apply);

    [Fact] void should_finish() => _failure.ShouldBeNull();
    [Fact] void should_ask_again_until_the_patch_is_done() => _calls.ShouldEqual(4);
    [Fact] void should_not_count_the_waiting_against_the_relocation_attempts() => _calls.ShouldBeGreaterThan(_options.MaxAttempts - 2);
}
