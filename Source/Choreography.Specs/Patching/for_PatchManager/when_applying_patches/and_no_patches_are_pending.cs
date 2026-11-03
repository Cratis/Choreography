// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.when_applying_patches;

public class and_no_patches_are_pending : given.a_patch_manager
{
    PatchOutcome _result;

    void Establish() => _patches.Clear();

    async Task Because()
    {
        await CreateManager();
        _result = await _manager.ApplyPatchesFor(_version);
    }

    [Fact] void should_complete() => _result.ShouldEqual(PatchOutcome.Completed);
    [Fact] async Task should_record_the_host_version() => await _storage.Received(1).SetHostVersion(_version);
    [Fact] async Task should_not_change_the_patched_version() => await _storage.DidNotReceive().SetPatchedVersion(Arg.Any<SemanticVersion>());
}
