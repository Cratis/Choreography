// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_decision_selects_an_outlet : given.a_coordinator
{
    void Establish()
    {
        Admit(
            [Trigger(), Decider("route", "yes", "no"), Action("yes-path"), Action("no-path")],
            [Edge("trigger", "next", "route"), Edge("route", "yes", "yes-path"), Edge("route", "no", "no-path")]);
        Succeed(Root("route"), "yes");
    }

    void Because() => Succeed(Root("yes-path"));

    [Fact] void should_only_have_run_the_selected_branch() => History.OfType<WorkflowNodeScheduled>().Select(scheduled => scheduled.Execution).ShouldNotContain(Root("no-path"));
    [Fact] void should_record_the_other_branch_as_unreachable() => Recorded<WorkflowNodesUnreachable>().Single().Executions.ShouldContainOnly(Root("no-path"));
    [Fact] void should_finish_the_run() => Recorded<WorkflowRunSucceeded>().ShouldNotBeEmpty();
}
