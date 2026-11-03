// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_the_host_version_is_not_configured : given.a_patch_runner
{
    void Establish()
    {
        _options.HostVersion = null;
        _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(PatchOutcome.Completed);
    }

    async Task Because() => await Apply();

    [Fact] async Task should_ask_with_the_version_of_the_running_host() => await _manager.Received(1).ApplyPatchesFor(HostVersion.Current);
}
