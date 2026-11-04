// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.when_applying_patches;

public class and_the_patch_fails : given.a_patch_manager
{
    Exception _error;

    void Establish() => _patch.Run = () => Task.FromException(new InvalidOperationException("patch failed"));

    async Task Because()
    {
        await CreateManager();
        _error = await Catch.Exception(() => _manager.ApplyPatchesFor(_version));
    }

    [Fact] void should_fail_the_call() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_not_record_the_patch() => await _storage.DidNotReceive().Save(Arg.Any<AppliedPatch>());
    [Fact] async Task should_not_record_a_host_version() => await _storage.DidNotReceive().SetHostVersion(Arg.Any<SemanticVersion>());
    [Fact] async Task should_not_change_the_patched_version() => await _storage.DidNotReceive().SetPatchedVersion(Arg.Any<SemanticVersion>());
}
