// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_SqlPatchStorage;

public class when_recording_patches_in_sql_server : given.a_sql_server_patch_database
{
    static readonly DateTimeOffset _appliedAt = new(2026, 9, 1, 12, 30, 15, TimeSpan.FromHours(2));
    SemanticVersion? _patchedOnFirstRead;
    SemanticVersion? _patched;
    SemanticVersion? _host;
    bool _has;
    bool _hasOther;
    List<AppliedPatch> _all;

    async Task Because()
    {
        _patchedOnFirstRead = await _storage.GetPatchedVersion();
        await _storage.Save(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 3, "rc.1"), _appliedAt));
        await _storage.Save(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 4), _appliedAt.AddDays(1)));
        await _storage.SetPatchedVersion(new SemanticVersion(2, 0, 0));
        await _storage.SetPatchedVersion(new SemanticVersion(2, 1, 0));
        await _storage.SetHostVersion(new SemanticVersion(3, 0, 0));

        var restarted = CreateStorage();
        _has = await restarted.Has("a-patch");
        _hasOther = await restarted.Has("another-patch");
        _all = [.. await restarted.GetAll()];
        _patched = await restarted.GetPatchedVersion();
        _host = await restarted.GetHostVersion();
    }

    [Fact] void should_not_have_a_version_before_anything_was_recorded() => _patchedOnFirstRead.ShouldBeNull();
    [Fact] void should_have_the_patch() => _has.ShouldBeTrue();
    [Fact] void should_not_have_another_patch() => _hasOther.ShouldBeFalse();
    [Fact] void should_keep_one_record_of_the_patch() => _all.Count.ShouldEqual(1);
    [Fact] void should_keep_the_latest_version_of_the_patch() => _all[0].Version.ShouldEqual(new SemanticVersion(1, 2, 4));
    [Fact] void should_keep_the_moment_the_patch_was_applied() => _all[0].AppliedAt.UtcDateTime.ShouldEqual(_appliedAt.AddDays(1).UtcDateTime);
    [Fact] void should_keep_the_latest_patched_version() => _patched.ShouldEqual(new SemanticVersion(2, 1, 0));
    [Fact] void should_keep_the_host_version_apart() => _host.ShouldEqual(new SemanticVersion(3, 0, 0));
}
