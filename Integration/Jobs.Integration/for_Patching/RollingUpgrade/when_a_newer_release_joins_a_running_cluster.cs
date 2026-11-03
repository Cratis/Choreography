// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_Patching.RollingUpgrade;

/// <summary>
/// The rolling deployment, start to finish: release 1.5 runs and has patched the store; 2.0 joins while 1.5 keeps running and
/// applies only what is new; then a 1.5 pod restarts in the middle of the rollout and must neither undo nor repeat anything.
/// </summary>
[Collection(JobsClusterCollection.Name)]
public class when_a_newer_release_joins_a_running_cluster : Specification
{
    static readonly SemanticVersion _oldVersion = new(1, 5, 0);
    static readonly SemanticVersion _newVersion = new(2, 0, 0);
    static readonly Release _oldRelease = new(_oldVersion, new Dictionary<string, SemanticVersion> { ["rename-orders"] = new(1, 5, 0) });
    static readonly Release _newRelease = new(_newVersion, new Dictionary<string, SemanticVersion>
    {
        ["rename-orders"] = new(1, 5, 0),
        ["split-customers"] = new(2, 0, 0)
    });

    RollingUpgradeClusterFixture _fixture;
    List<(string Silo, string Patch)> _afterOldRelease;
    List<(string Silo, string Patch)> _afterNewRelease;
    List<(string Silo, string Patch)> _afterOldPodRestarted;
    SemanticVersion? _hostVersionAfterNewRelease;
    SemanticVersion? _hostVersionAfterOldPodRestarted;
    SemanticVersion? _patchedVersion;
    List<string> _silosAfterNewRelease;
    List<string> _silosAtTheEnd;

    void Establish() => _fixture = new(new Dictionary<string, Release>
    {
        ["Primary"] = _oldRelease
    });

    async Task Because()
    {
        _afterOldRelease = [.. _fixture.Signals.Applied];

        await _fixture.Join("new-2.0", _newRelease);
        _afterNewRelease = [.. _fixture.Signals.Applied];
        _hostVersionAfterNewRelease = await _fixture.Storage.GetHostVersion();
        _silosAfterNewRelease = [.. _fixture.RunningSilos];

        await _fixture.Join("old-1.5-restarted", _oldRelease);
        _afterOldPodRestarted = [.. _fixture.Signals.Applied];
        _hostVersionAfterOldPodRestarted = await _fixture.Storage.GetHostVersion();
        _patchedVersion = await _fixture.Storage.GetPatchedVersion();
        _silosAtTheEnd = [.. _fixture.RunningSilos];
    }

    [Fact] void should_have_let_the_old_release_patch_the_store_when_it_started() => _afterOldRelease.ShouldEqual([("Primary", "rename-orders")]);
    [Fact] void should_apply_only_the_new_patch_when_the_new_release_joins() => _afterNewRelease.ShouldEqual([("Primary", "rename-orders"), ("new-2.0", "split-customers")]);
    [Fact] void should_apply_the_new_patch_on_the_new_release() => _afterNewRelease[^1].Silo.ShouldEqual("new-2.0");
    [Fact] void should_mark_the_store_with_the_new_release() => _hostVersionAfterNewRelease.ShouldEqual(_newVersion);
    [Fact] void should_not_have_stopped_the_old_release_to_do_it() => _silosAfterNewRelease.ShouldEqual(["Primary", "Secondary_1"]);
    [Fact] void should_apply_nothing_when_an_old_pod_restarts_during_the_rollout() => _afterOldPodRestarted.ShouldEqual(_afterNewRelease);
    [Fact] void should_keep_the_store_marked_with_the_new_release() => _hostVersionAfterOldPodRestarted.ShouldEqual(_newVersion);
    [Fact] void should_keep_the_patched_version_at_the_newest_patch() => _patchedVersion.ShouldEqual(_newVersion);
    [Fact] void should_end_with_all_three_silos_running() => _silosAtTheEnd.ShouldEqual(["Primary", "Secondary_1", "Secondary_2"]);
}
