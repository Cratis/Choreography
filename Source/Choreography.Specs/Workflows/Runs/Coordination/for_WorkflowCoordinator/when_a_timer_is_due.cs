// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_timer_is_due : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Timer("timer", TimeSpan.FromMinutes(5)), Action("next")], [Edge("trigger", "next", "timer"), Edge("timer", "elapsed", "next")]);

    void Because() => ExpireWaitsAt(Now + TimeSpan.FromMinutes(5));

    [Fact] void should_resolve_the_wait_through_elapsed() => Recorded<WorkflowWaitResolved>().Single().Outlet.Value.ShouldEqual("elapsed");
    [Fact] void should_resolve_it_as_the_system() => Recorded<WorkflowWaitResolved>().Single().Actor.ShouldEqual(WorkflowActor.System);
    [Fact] void should_leave_through_elapsed() => State.Executions[Root("timer")].Outlet.Value.ShouldEqual("elapsed");
    [Fact] void should_schedule_the_next_node() => ScheduledNow.ShouldContainOnly(Root("next"));
}
