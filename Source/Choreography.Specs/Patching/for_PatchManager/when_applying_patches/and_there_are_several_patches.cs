// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchManager.when_applying_patches;

public class and_there_are_several_patches : given.a_patch_manager
{
    readonly List<string> _order = [];
    given.FakePatch _newest;
    given.FakePatch _oldest;

    void Establish()
    {
        _patches.Clear();
        _newest = APatch("newest", new SemanticVersion(19, 95, 0));
        _oldest = APatch("oldest", new SemanticVersion(19, 80, 0));
        _newest.Run = () =>
        {
            _order.Add("newest");
            return Task.CompletedTask;
        };
        _oldest.Run = () =>
        {
            _order.Add("oldest");
            return Task.CompletedTask;
        };
        _patches.AddRange([_newest, _oldest]);
    }

    async Task Because()
    {
        await CreateManager();
        await _manager.ApplyPatchesFor(_version);
    }

    [Fact] void should_apply_them_oldest_first() => _order.ShouldEqual(["oldest", "newest"]);
    [Fact] async Task should_record_the_newest_version_as_patched() => await _storage.Received(1).SetPatchedVersion(_newest.Version);
}
