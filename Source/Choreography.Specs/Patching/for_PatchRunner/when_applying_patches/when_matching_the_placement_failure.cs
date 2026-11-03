// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class when_matching_the_placement_failure : Specification
{
    [Fact] void should_match_the_patch_manager_placement_failure() => PatchRunner.IsAwaitingManifest(new OrleansException("No active nodes are compatible with grain cratis-choreography-patch-manager and interface Cratis.Choreography.Patching.IPatchManager version 0. Known nodes with grain type: none.")).ShouldBeTrue();
    [Fact] void should_reject_a_different_grain_type() => PatchRunner.IsAwaitingManifest(new OrleansException("No active nodes are compatible with grain observer and interface Cratis.Choreography.Patching.IPatchManager version 0.")).ShouldBeFalse();
    [Fact] void should_reject_unrelated_orleans_errors() => PatchRunner.IsAwaitingManifest(new OrleansException("placement failed")).ShouldBeFalse();
}
