// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_Patching.RollingUpgrade;

/// <summary>
/// The pod that holds the patch manager (the test host's primary silo keeps the cluster's membership, so a secondary plays it) is evicted just as the new release is starting - the ordinary hazard of a rolling
/// deployment. The new release must still come up and patch the store, exactly once.
/// </summary>
[Collection(JobsClusterCollection.Name)]
public class when_the_silo_holding_the_patch_manager_crashes_during_a_rollout : Specification
{
    static readonly Release _oldRelease = new(new SemanticVersion(1, 5, 0), new Dictionary<string, SemanticVersion> { ["rename-orders"] = new(1, 5, 0) });
    static readonly Release _newRelease = new(new SemanticVersion(2, 0, 0), new Dictionary<string, SemanticVersion>
    {
        ["rename-orders"] = new(1, 5, 0),
        ["split-customers"] = new(2, 0, 0)
    });

    RollingUpgradeClusterFixture _fixture;
    Exception _failure;
    List<(string Silo, string Patch)> _applied;
    SemanticVersion? _hostVersion;

    void Establish() => _fixture = new(
        new Dictionary<string, Release>
        {
            ["Primary"] = _oldRelease with { PatchesOnStart = false },
            ["Secondary_1"] = _oldRelease
        },
        silos: 2);

    async Task Because()
    {
        await _fixture.Kill("Secondary_1");
        _failure = await Cratis.Specifications.Catch.Exception(() => _fixture.Join("new-2.0", _newRelease));
        _applied = [.. _fixture.Signals.Applied];
        _hostVersion = await _fixture.Storage.GetHostVersion();
    }

    [Fact] void should_let_the_new_release_start() => _failure.ShouldBeNull();
    [Fact] void should_apply_each_patch_exactly_once() => _applied.Select(_ => _.Patch).Order().ToArray().ShouldEqual(["rename-orders", "split-customers"]);
    [Fact] void should_apply_the_new_patch_on_the_new_release() => _applied.Single(_ => _.Patch == "split-customers").Silo.ShouldEqual("new-2.0");
    [Fact] void should_mark_the_store_with_the_new_release() => _hostVersion.ShouldEqual(new SemanticVersion(2, 0, 0));
}
