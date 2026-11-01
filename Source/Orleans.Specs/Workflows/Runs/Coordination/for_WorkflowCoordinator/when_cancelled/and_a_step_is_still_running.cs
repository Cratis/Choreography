// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_cancelled;

public class and_a_step_is_still_running : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a"), Action("b")], [Edge("trigger", "next", "a"), Edge("a", "next", "b")]);
        Cancel();
    }

    void Because() => Succeed(Root("a"));

    [Fact] void should_still_record_the_outcome() => Recorded<WorkflowNodeSucceeded>().Single().Execution.ShouldEqual(Root("a"));
    [Fact] void should_not_plan_anything_from_it() => ScheduledNow.ShouldBeEmpty();
    [Fact] void should_not_finish_the_run_again() => Recorded<WorkflowRunSucceeded>().ShouldBeEmpty();
}
