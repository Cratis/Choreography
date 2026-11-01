// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_signal_wait_expires : given.a_coordinator
{
    void Establish() => Admit([Trigger(), SignalWait("wait", TimeSpan.FromMinutes(1)), Action("late")], [Edge("trigger", "next", "wait"), Edge("wait", "expired", "late")]);

    void Because() => ExpireWaitsAt(Now + TimeSpan.FromMinutes(1));

    [Fact] void should_resolve_the_wait_through_expired() => Recorded<WorkflowWaitResolved>().Single().Outlet.Value.ShouldEqual("expired");
    [Fact] void should_schedule_the_expiry_path() => ScheduledNow.ShouldContainOnly(Root("late"));
}
