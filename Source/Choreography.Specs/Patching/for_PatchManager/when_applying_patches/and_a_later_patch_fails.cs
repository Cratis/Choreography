// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.when_applying_patches;

public class and_a_later_patch_fails : given.a_patch_manager
{
    given.FakePatch _failing;
    Exception _error;

    void Establish()
    {
        _failing = APatch("failing", new SemanticVersion(19, 95, 0));
        _failing.Run = () => Task.FromException(new InvalidOperationException("patch failed"));
        _patches.Add(_failing);
    }

    async Task Because()
    {
        await CreateManager();
        _error = await Catch.Exception(() => _manager.ApplyPatchesFor(_version));
    }

    [Fact] void should_fail_the_call() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_keep_the_record_of_the_patch_that_succeeded() => await _storage.Received(1).Save(Arg.Is<AppliedPatch>(_ => _.Name == _patch.Name));
    [Fact] async Task should_not_record_the_failing_patch() => await _storage.DidNotReceive().Save(Arg.Is<AppliedPatch>(_ => _.Name == _failing.Name));
    [Fact] async Task should_not_advance_the_patched_version() => await _storage.DidNotReceive().SetPatchedVersion(Arg.Any<SemanticVersion>());
    [Fact] async Task should_not_record_a_host_version() => await _storage.DidNotReceive().SetHostVersion(Arg.Any<SemanticVersion>());
}
