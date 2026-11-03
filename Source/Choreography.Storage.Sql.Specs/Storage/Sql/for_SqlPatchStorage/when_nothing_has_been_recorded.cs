// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Storage.Sql.for_SqlPatchStorage;

public class when_nothing_has_been_recorded : given.a_sqlite_patches_database
{
    SemanticVersion? _patched;
    SemanticVersion? _host;
    bool _has;
    List<AppliedPatch> _all;

    async Task Because()
    {
        _patched = await _storage.GetPatchedVersion();
        _host = await _storage.GetHostVersion();
        _has = await _storage.Has("a-patch");
        _all = [.. await _storage.GetAll()];
    }

    [Fact] void should_not_have_a_patched_version() => _patched.ShouldBeNull();
    [Fact] void should_not_have_a_host_version() => _host.ShouldBeNull();
    [Fact] void should_not_have_any_patch() => _has.ShouldBeFalse();
    [Fact] void should_return_no_patches() => _all.ShouldBeEmpty();
}
