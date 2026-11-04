// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_a_patch_fails : given.a_patch_runner
{
    Exception _failure;

    void Establish() => _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => Task.FromException<PatchOutcome>(new InvalidOperationException("patch failed")));

    async Task Because() => _failure = await Catch.Exception(Apply);

    [Fact] void should_fail_with_the_failure_of_the_patch() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_ask_once() => await _manager.Received(1).ApplyPatchesFor(_version);
    [Fact] void should_leave_no_caller_silo_in_the_request_context() => RequestContext.Get(PatchRelocation.CallerSiloKey).ShouldBeNull();
}
