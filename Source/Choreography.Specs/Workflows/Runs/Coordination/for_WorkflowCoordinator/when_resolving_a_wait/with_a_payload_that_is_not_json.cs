// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_resolving_a_wait;

public class with_a_payload_that_is_not_json : given.a_coordinator
{
    void Establish() => Admit([Trigger(), SignalWait("wait", TimeSpan.Zero), Action("next")], [Edge("trigger", "next", "wait"), Edge("wait", "resumed", "next")]);

    void Because() => ResolveWaitOf(Root("wait"), "resumed", "not json");

    [Fact] void should_not_record_resolution() => Recorded<WorkflowWaitResolved>().ShouldBeEmpty();
    [Fact] void should_keep_the_wait_open() => State.OpenWaits.Count.ShouldEqual(1);
    [Fact] void should_not_schedule_the_next_node() => ScheduledNow.ShouldBeEmpty();
}
