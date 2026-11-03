// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.when_applying_patches;

public class and_patches_have_already_been_recorded : given.a_patch_manager
{
    void Establish() => _storage.Has(_patch.Name).Returns(true);

    async Task Because()
    {
        await CreateManager();
        await _manager.ApplyPatchesFor(_version);
    }

    [Fact] void should_not_apply_the_patch_again() => _patch.UpCalls.ShouldEqual(0);
    [Fact] async Task should_record_the_host_version() => await _storage.Received(1).SetHostVersion(_version);
    [Fact] async Task should_not_change_the_patched_version() => await _storage.DidNotReceive().SetPatchedVersion(Arg.Any<SemanticVersion>());
}
