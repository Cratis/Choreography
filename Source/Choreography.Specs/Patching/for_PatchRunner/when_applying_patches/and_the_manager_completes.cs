// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_the_manager_completes : given.a_patch_runner
{
    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(PatchOutcome.Completed);

    async Task Because() => await Apply();

    [Fact] async Task should_ask_with_the_host_version() => await _manager.Received(1).ApplyPatchesFor(_version);
    [Fact] void should_not_wait() => _timeProvider.Delays.ShouldBeEmpty();
    [Fact] void should_leave_no_caller_silo_in_the_request_context() => RequestContext.Get(PatchRelocation.CallerSiloKey).ShouldBeNull();
}
