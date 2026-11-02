// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_a_rehydration_finishes_while_a_recovery_is_running : given.a_recovery_alongside_a_rehydration
{
    int _releasesWhileTheRecoveryWasRunning;

    async Task Because()
    {
        CompleteResumes();
        await WaitForRehydration();
        await Task.Delay(200);
        _releasesWhileTheRecoveryWasRunning = Releases;
        CompleteTheRecovery();
        await WaitForARelease();
    }

    [Fact] void should_not_release_the_activation_while_the_recovery_is_running() => _releasesWhileTheRecoveryWasRunning.ShouldEqual(0);
    [Fact] void should_release_the_activation_once_the_recovery_has_finished() => Releases.ShouldEqual(1);
    [Fact] void should_take_hold_of_the_activation_once() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DelayDeactivation(It.IsAny<IGrainContext>(), Timeout.InfiniteTimeSpan), Times.Once);
}
