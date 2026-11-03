// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.when_applying_patches;

public class and_this_host_is_newest : given.a_patch_manager
{
    PatchOutcome _result;

    void Establish() => _storage.GetHostVersion().Returns(new SemanticVersion(19, 99, 0));

    async Task Because()
    {
        await CreateManager();
        _result = await _manager.ApplyPatchesFor(_version);
    }

    [Fact] void should_complete() => _result.ShouldEqual(PatchOutcome.Completed);
    [Fact] void should_apply_the_patch() => _patch.UpCalls.ShouldEqual(1);
    [Fact] async Task should_record_the_patch() => await _storage.Received(1).Save(Arg.Is<AppliedPatch>(_ => _.Name == _patch.Name && _.Version == _patch.Version));
    [Fact] async Task should_record_the_host_version() => await _storage.Received(1).SetHostVersion(_version);
    [Fact] async Task should_record_the_patched_version_separately() => await _storage.Received(1).SetPatchedVersion(_patch.Version);
}
