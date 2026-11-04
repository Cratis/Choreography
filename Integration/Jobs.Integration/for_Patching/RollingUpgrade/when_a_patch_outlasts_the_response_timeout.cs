// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_Patching.RollingUpgrade;

/// <summary>
/// A data migration that takes longer than a grain call is allowed to wait. The patch keeps running in the grain; the host that
/// asked for it must keep waiting rather than fail its start, and the patch must not run twice.
/// </summary>
[Collection(JobsClusterCollection.Name)]
public class when_a_patch_outlasts_the_response_timeout : Specification
{
    static readonly Release _release = new(new SemanticVersion(2, 0, 0), new Dictionary<string, SemanticVersion> { ["split-customers"] = new(2, 0, 0) });

    RollingUpgradeClusterFixture _fixture;
    Exception _failure;
    List<(string Silo, string Patch)> _applied;
    SemanticVersion? _patchedVersion;

    void Establish() => _fixture = new(
        new Dictionary<string, Release> { ["Primary"] = _release, ["Secondary_1"] = _release },
        patchDuration: TimeSpan.FromSeconds(6),
        responseTimeout: TimeSpan.FromSeconds(2),
        silos: 2);

    async Task Because()
    {
        _failure = await Cratis.Specifications.Catch.Exception(() => _fixture.Join("joiner", _release));
        _applied = [.. _fixture.Signals.Applied];
        _patchedVersion = await _fixture.Storage.GetPatchedVersion();
    }

    [Fact] void should_let_the_host_start() => _failure.ShouldBeNull();
    [Fact] void should_apply_the_patch_exactly_once() => _applied.Count.ShouldEqual(1);
    [Fact] void should_record_the_patch() => _patchedVersion.ShouldEqual(new SemanticVersion(2, 0, 0));
}
