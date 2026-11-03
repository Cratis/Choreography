// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_the_manager_relocates_once : given.a_patch_runner
{
    int _calls;

    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => Task.FromResult(++_calls < 2 ? PatchOutcome.Relocate : PatchOutcome.Completed));

    async Task Because() => await Apply();

    [Fact] void should_ask_again() => _calls.ShouldEqual(2);
    [Fact] void should_wait_one_second_before_asking_again() => _timeProvider.Delays.ShouldEqual([TimeSpan.FromSeconds(1)]);
}
