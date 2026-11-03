// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_MongoDBPatchStorage;

public class when_saving_a_patch_that_is_already_recorded : given.a_mongodb_patch_storage
{
    static readonly DateTimeOffset _later = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
    List<AppliedPatch> _all;

    async Task Because()
    {
        await _storage.Save(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 3), _later.AddDays(-1)));
        await _storage.Save(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 4), _later));
        _all = [.. await _storage.GetAll()];
    }

    [Fact] void should_keep_one_record() => _all.Count.ShouldEqual(1);
    [Fact] void should_keep_the_latest_version() => _all[0].Version.ShouldEqual(new SemanticVersion(1, 2, 4));
    [Fact] void should_keep_the_latest_moment() => _all[0].AppliedAt.UtcDateTime.ShouldEqual(_later.UtcDateTime);
}
