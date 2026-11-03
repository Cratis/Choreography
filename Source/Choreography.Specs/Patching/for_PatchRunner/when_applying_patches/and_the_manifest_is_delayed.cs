// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_the_manifest_is_delayed : given.a_patch_runner
{
    int _calls;
    Exception _failure;

    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => ++_calls < 3
        ? Task.FromException<PatchOutcome>(AwaitingManifest())
        : Task.FromResult(PatchOutcome.Completed));

    async Task Because() => _failure = await Catch.Exception(Apply);

    [Fact] void should_finish() => _failure.ShouldBeNull();
    [Fact] void should_retry_until_the_manifest_arrives() => _calls.ShouldEqual(3);
}
