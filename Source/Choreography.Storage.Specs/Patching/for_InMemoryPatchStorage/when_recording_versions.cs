// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage;

namespace Cratis.Choreography.Patching.for_InMemoryPatchStorage;

public class when_recording_versions : Specification
{
    InMemoryPatchStorage _storage;
    SemanticVersion? _patched;
    SemanticVersion? _host;

    void Establish() => _storage = new();

    async Task Because()
    {
        await _storage.SetPatchedVersion(new SemanticVersion(1, 0, 0));
        await _storage.SetPatchedVersion(new SemanticVersion(2, 0, 0));
        await _storage.SetHostVersion(new SemanticVersion(3, 0, 0));
        _patched = await _storage.GetPatchedVersion();
        _host = await _storage.GetHostVersion();
    }

    [Fact] void should_return_the_latest_patched_version() => _patched.ShouldEqual(new SemanticVersion(2, 0, 0));
    [Fact] void should_keep_the_host_version_apart() => _host.ShouldEqual(new SemanticVersion(3, 0, 0));
}
