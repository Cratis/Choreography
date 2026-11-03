// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_Patching;

public class when_the_silo_starts(PatchingSiloFixture context) : Given<PatchingSiloFixture>(context)
{
    [Fact] void should_apply_the_patches_oldest_first() => Context.Probe.Applied.ToArray().ShouldEqual([nameof(TheOlderIntegrationPatch), nameof(TheNewerIntegrationPatch)]);

    [Fact]
    async Task should_record_the_patches()
    {
        var recorded = (await Context.Storage.GetAll()).Select(_ => _.Name).Order().ToArray();
        recorded.ShouldEqual([nameof(TheNewerIntegrationPatch), nameof(TheOlderIntegrationPatch)]);
    }

    [Fact] async Task should_record_how_far_patching_has_come() => (await Context.Storage.GetPatchedVersion()).ShouldEqual(new SemanticVersion(1, 6, 0));
    [Fact] async Task should_mark_the_store_with_the_host_version() => (await Context.Storage.GetHostVersion()).ShouldEqual(PatchingSiloFixture.HostVersion);
}
