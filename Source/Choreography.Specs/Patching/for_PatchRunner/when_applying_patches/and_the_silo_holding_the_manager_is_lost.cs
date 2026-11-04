// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_the_silo_holding_the_manager_is_lost : given.a_patch_runner
{
    int _calls;
    Exception _failure;

    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => ++_calls switch
    {
        1 => Task.FromException<PatchOutcome>(new SiloUnavailableException("The silo is gone")),
        2 => Task.FromException<PatchOutcome>(Rejection("Rejected while the silo was shutting down")),
        _ => Task.FromResult(PatchOutcome.Completed)
    });

    async Task Because() => _failure = await Cratis.Specifications.Catch.Exception(Apply);

    [Fact] void should_finish() => _failure.ShouldBeNull();
    [Fact] void should_ask_again_after_each_failure() => _calls.ShouldEqual(3);
}
