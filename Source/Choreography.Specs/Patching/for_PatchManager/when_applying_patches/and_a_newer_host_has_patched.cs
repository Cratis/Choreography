// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.when_applying_patches;

public class and_a_newer_host_has_patched : given.a_patch_manager
{
    PatchOutcome _result;

    void Establish() => _storage.GetHostVersion().Returns(new SemanticVersion(19, 101, 0));

    async Task Because()
    {
        await CreateManager();
        _result = await _manager.ApplyPatchesFor(_version);
    }

    [Fact] void should_complete() => _result.ShouldEqual(PatchOutcome.Completed);
    [Fact] void should_not_apply_the_patch() => _patch.UpCalls.ShouldEqual(0);
    [Fact] async Task should_not_change_the_host_version() => await _storage.DidNotReceive().SetHostVersion(Arg.Any<SemanticVersion>());
    [Fact] async Task should_not_read_the_patched_version() => await _storage.DidNotReceive().GetPatchedVersion();
}
