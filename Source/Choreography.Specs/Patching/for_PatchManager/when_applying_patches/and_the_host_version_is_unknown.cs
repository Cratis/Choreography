// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.when_applying_patches;

public class and_the_host_version_is_unknown : given.a_patch_manager
{
    void Establish()
    {
        _version = new SemanticVersion(1, 0, 0);
        _storage.GetHostVersion().Returns(new SemanticVersion(19, 101, 0));
    }

    async Task Because()
    {
        await CreateManager();
        await _manager.ApplyPatchesFor(_version);
    }

    [Fact] void should_apply_the_patch() => _patch.UpCalls.ShouldEqual(1);
    [Fact] async Task should_not_record_a_host_version() => await _storage.DidNotReceive().SetHostVersion(Arg.Any<SemanticVersion>());
}
