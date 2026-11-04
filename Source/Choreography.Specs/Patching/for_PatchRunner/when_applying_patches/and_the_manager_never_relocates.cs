// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_the_manager_never_relocates : given.a_patch_runner
{
    Exception _failure;

    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(PatchOutcome.Relocate);

    async Task Because() => _failure = await Catch.Exception(Apply);

    [Fact] void should_fail() => _failure.ShouldBeOfExactType<PatchManagerCouldNotRelocate>();
    [Fact] void should_name_the_host_version() => _failure.Message.ShouldContain(_version.ToString());
    [Fact] async Task should_stop_after_the_bounded_attempts() => await _manager.Received(5).ApplyPatchesFor(_version);
    [Fact] void should_back_off_exponentially() => _timeProvider.Delays.ShouldEqual([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)]);
    [Fact] void should_leave_no_caller_silo_in_the_request_context() => RequestContext.Get(PatchRelocation.CallerSiloKey).ShouldBeNull();
}
