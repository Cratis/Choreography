// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_cancelling_with_an_open_wait : given.a_coordinator
{
    void Establish() => Admit([Trigger(), SignalWait("wait", TimeSpan.Zero)], [Edge("trigger", "next", "wait")]);

    void Because() => Cancel();

    [Fact] void should_cancel_the_wait() => Recorded<WorkflowWaitCancelled>().Count().ShouldEqual(1);
    [Fact] void should_close_the_wait() => State.OpenWaits.ShouldBeEmpty();
    [Fact] void should_record_cancellation_last() => Decision.Events.Last().ShouldBeOfExactType<WorkflowRunCancelled>();
}
