// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_the_manifest_never_arrives : given.a_patch_runner
{
    Exception _failure;

    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => Task.FromException<PatchOutcome>(AwaitingManifest()));

    async Task Because() => _failure = await Catch.Exception(Apply);

    [Fact] void should_fail() => _failure.ShouldBeOfExactType<PatchManagerCouldNotRelocate>();
    [Fact] async Task should_stop_after_the_bounded_attempts() => await _manager.Received(5).ApplyPatchesFor(_version);
}
