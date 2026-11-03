// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_startup_is_cancelled : given.a_patch_runner
{
    Exception _failure;

    void Establish()
    {
        _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(PatchOutcome.Relocate);
        _cancellation.Cancel();
    }

    async Task Because() => _failure = await Catch.Exception(Apply);

    [Fact] void should_stop() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] async Task should_not_ask_the_manager() => await _manager.DidNotReceive().ApplyPatchesFor(Arg.Any<SemanticVersion>());
}
