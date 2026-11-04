// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_another_grain_has_no_compatible_silo : given.a_patch_runner
{
    Exception _failure;

    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => Task.FromException<PatchOutcome>(
        new OrleansException("No active nodes are compatible with grain observer and interface Cratis.Choreography.Patching.IPatchManager version 0.")));

    async Task Because() => _failure = await Catch.Exception(Apply);

    [Fact] void should_not_retry_what_is_not_the_patch_manager() => _failure.ShouldBeOfExactType<OrleansException>();
    [Fact] async Task should_ask_once() => await _manager.Received(1).ApplyPatchesFor(_version);
}
