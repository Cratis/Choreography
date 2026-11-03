// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_a_recovery_finishes_while_a_rehydration_is_draining : given.a_recovery_alongside_a_rehydration
{
    int _releasesWhileTheDrainWasPending;

    async Task Because()
    {
        CompleteTheRecovery();
        await WaitForKeepAliveHolders(1);
        _releasesWhileTheDrainWasPending = Releases;
        CompleteResumes();
        await WaitForRehydration();
        await WaitForARelease();
    }

    [Fact] void should_not_release_the_activation_while_the_drain_is_pending() => _releasesWhileTheDrainWasPending.ShouldEqual(0);
    [Fact] void should_release_the_activation_once_the_drain_has_finished() => Releases.ShouldEqual(1);
    [Fact] void should_take_hold_of_the_activation_once() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DelayDeactivation(It.IsAny<IGrainContext>(), Timeout.InfiniteTimeSpan), Times.Once);
}
