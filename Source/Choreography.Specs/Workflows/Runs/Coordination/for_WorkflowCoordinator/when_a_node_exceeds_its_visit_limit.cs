// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_node_exceeds_its_visit_limit : given.a_coordinator
{
    static readonly WorkflowNodePolicy _twice = WorkflowNodePolicy.Default with { MaxVisits = 2 };

    void Establish()
    {
        Admit(
            [Trigger(), Action("draft", _twice), Action("review", _twice, "next", "again")],
            [Edge("trigger", "next", "draft"), Edge("draft", "next", "review"), Edge("review", "again", "draft")]);
        Succeed(Root("draft"));
        Succeed(Root("review"), "again");
        Succeed(Root("draft", 2));
    }

    void Because() => Succeed(Root("review", 2), "again");

    [Fact] void should_fail_the_third_visit() => Recorded<WorkflowNodeFailed>().Single().Execution.ShouldEqual(Root("draft", 3));
    [Fact] void should_fail_it_for_exceeding_the_limit() => Recorded<WorkflowNodeFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.VisitLimitExceeded);
    [Fact] void should_not_schedule_it() => ScheduledNow.ShouldBeEmpty();
}
