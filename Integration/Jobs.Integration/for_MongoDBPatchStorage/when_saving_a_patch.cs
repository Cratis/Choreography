// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_MongoDBPatchStorage;

public class when_saving_a_patch : given.a_mongodb_patch_storage
{
    static readonly DateTimeOffset _appliedAt = new(2026, 9, 1, 12, 30, 15, TimeSpan.FromHours(2));
    bool _has;
    bool _hasOther;
    List<AppliedPatch> _all;

    async Task Because()
    {
        await _storage.Save(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 3, "rc.1"), _appliedAt));
        _has = await _storage.Has("a-patch");
        _hasOther = await _storage.Has("another-patch");
        _all = [.. await _storage.GetAll()];
    }

    [Fact] void should_have_the_patch() => _has.ShouldBeTrue();
    [Fact] void should_not_have_another_patch() => _hasOther.ShouldBeFalse();
    [Fact] void should_return_one_patch() => _all.Count.ShouldEqual(1);
    [Fact] void should_keep_the_name() => _all[0].Name.ShouldEqual("a-patch");
    [Fact] void should_keep_the_version_with_its_pre_release() => _all[0].Version.ShouldEqual(new SemanticVersion(1, 2, 3, "rc.1"));
    [Fact] void should_keep_the_moment_it_was_applied() => _all[0].AppliedAt.UtcDateTime.ShouldEqual(_appliedAt.UtcDateTime);
}
