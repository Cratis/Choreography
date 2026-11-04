// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_a_patch_times_out_by_itself : given.a_patch_runner
{
    Exception _failure;

    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => Task.FromException<PatchOutcome>(new TimeoutException("The database did not answer")));

    async Task Because() => _failure = await Cratis.Specifications.Catch.Exception(Apply);

    [Fact] void should_fail_the_start_with_the_failure_of_the_patch() => _failure.ShouldBeOfExactType<TimeoutException>();
    [Fact] async Task should_not_ask_again() => await _manager.Received(1).ApplyPatchesFor(_version);
}
