// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching.for_PatchRunner.when_applying_patches;

public class and_patching_never_completes : given.a_patch_runner
{
    Exception _failure;

    void Establish()
    {
        _options.MaxWait = TimeSpan.FromMinutes(2);
        _manager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => Task.FromException<PatchOutcome>(
            new TimeoutException(PatchRunner.ResponseTimeoutPrefix + " in 00:00:30 for message: Request")));
    }

    async Task Because() => _failure = await Cratis.Specifications.Catch.Exception(Apply);

    [Fact] void should_give_up() => _failure.ShouldBeOfExactType<PatchingDidNotComplete>();
    [Fact] void should_name_the_host_version() => _failure.Message.ShouldContain(_version.ToString());
    [Fact] void should_keep_the_last_failure() => _failure.InnerException.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_stop_asking_once_the_wait_is_used_up() => _timeProvider.Delays.Sum(_ => _.TotalSeconds).ShouldBeGreaterThanOrEqual(120d);
    [Fact] void should_not_wait_much_longer_than_allowed() => _timeProvider.Delays.Sum(_ => _.TotalSeconds).ShouldBeLessThan(150d);
}
