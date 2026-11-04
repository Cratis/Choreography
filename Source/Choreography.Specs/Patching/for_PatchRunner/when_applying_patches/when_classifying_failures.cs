// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class when_classifying_failures : given.a_patch_runner
{
    [Fact] void should_retry_an_orleans_response_timeout() => PatchRunner.IsTransient(new TimeoutException(PatchRunner.ResponseTimeoutPrefix + " in 00:00:30")).ShouldBeTrue();
    [Fact] void should_retry_an_unavailable_silo() => PatchRunner.IsTransient(new SiloUnavailableException("gone")).ShouldBeTrue();
    [Fact] void should_retry_a_rejected_message() => PatchRunner.IsTransient(Rejection("rejected")).ShouldBeTrue();
    [Fact] void should_not_retry_a_timeout_the_patch_raised() => PatchRunner.IsTransient(new TimeoutException("The database did not answer")).ShouldBeFalse();
    [Fact] void should_not_retry_the_failure_of_a_patch() => PatchRunner.IsTransient(new InvalidOperationException("patch failed")).ShouldBeFalse();
    [Fact] void should_not_retry_a_cancellation() => PatchRunner.IsTransient(new OperationCanceledException()).ShouldBeFalse();
}
