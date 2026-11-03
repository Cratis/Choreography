// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.when_applying_patches;

public class and_the_caller_version_is_unknown : given.a_patch_manager
{
    PatchOutcome _result;

    async Task Because()
    {
        await CreateManager();
        _result = await _manager.ApplyPatchesFor(SemanticVersion.NotSet);
    }

    [Fact] void should_complete() => _result.ShouldEqual(PatchOutcome.Completed);
    [Fact] void should_apply_the_patch() => _patch.UpCalls.ShouldEqual(1);
}
