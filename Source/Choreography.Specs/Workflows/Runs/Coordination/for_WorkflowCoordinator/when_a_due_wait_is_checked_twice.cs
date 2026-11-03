// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_due_wait_is_checked_twice : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Timer("timer", TimeSpan.FromMinutes(5)), Action("next")], [Edge("trigger", "next", "timer"), Edge("timer", "elapsed", "next")]);
        ExpireWaitsAt(Now + TimeSpan.FromMinutes(5));
    }

    void Because() => ExpireWaitsAt(Now + TimeSpan.FromMinutes(6));

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
    [Fact] void should_have_resolved_the_wait_once() => History.OfType<WorkflowWaitResolved>().Count().ShouldEqual(1);
}
