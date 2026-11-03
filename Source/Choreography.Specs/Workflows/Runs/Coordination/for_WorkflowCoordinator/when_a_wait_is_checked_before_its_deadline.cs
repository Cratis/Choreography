// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_wait_is_checked_before_its_deadline : given.a_coordinator
{
    void Establish() => Admit([Trigger(), SignalWait("wait", TimeSpan.FromMinutes(1)), Action("late")], [Edge("trigger", "next", "wait"), Edge("wait", "expired", "late")]);

    void Because() => ExpireWaitsAt(Now + TimeSpan.FromSeconds(59));

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
    [Fact] void should_keep_waiting() => StatusOf(Root("wait")).ShouldEqual(NodeExecutionStatus.Waiting);
}
