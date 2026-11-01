// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_signal_wait_is_resolved : given.a_coordinator
{
    void Establish() => Admit([Trigger(), SignalWait("wait", TimeSpan.Zero), Action("next")], [Edge("trigger", "next", "wait"), Edge("wait", "resumed", "next")]);

    void Because() => ResolveWaitOf(Root("wait"), "resumed", "{\"answer\":42}");

    [Fact] void should_record_resolution() => Recorded<WorkflowWaitResolved>().Count().ShouldEqual(1);
    [Fact] void should_close_the_wait() => State.OpenWaits.ShouldBeEmpty();
    [Fact] void should_schedule_the_next_node() => ScheduledNow.ShouldContainOnly(Root("next"));
}
