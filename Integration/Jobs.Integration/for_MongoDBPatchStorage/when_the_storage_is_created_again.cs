// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_MongoDBPatchStorage;

public class when_the_storage_is_created_again : given.a_mongodb_patch_storage
{
    SemanticVersion? _patched;
    SemanticVersion? _host;
    bool _has;

    async Task Because()
    {
        await _storage.Save(new AppliedPatch("a-patch", new SemanticVersion(1, 2, 3), DateTimeOffset.UtcNow));
        await _storage.SetPatchedVersion(new SemanticVersion(2, 0, 0));
        await _storage.SetPatchedVersion(new SemanticVersion(2, 1, 0));
        await _storage.SetHostVersion(new SemanticVersion(3, 0, 0));

        var restarted = CreateStorage();
        _has = await restarted.Has("a-patch");
        _patched = await restarted.GetPatchedVersion();
        _host = await restarted.GetHostVersion();
    }

    [Fact] void should_still_have_the_patch() => _has.ShouldBeTrue();
    [Fact] void should_still_have_the_latest_patched_version() => _patched.ShouldEqual(new SemanticVersion(2, 1, 0));
    [Fact] void should_keep_the_host_version_apart() => _host.ShouldEqual(new SemanticVersion(3, 0, 0));
}
