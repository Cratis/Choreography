// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_fanning_out;

public class with_two_visits_and_repeated_item_keys : given.a_fan_out
{
    NodeExecutionKey _second = null!;

    void Establish()
    {
        var policy = WorkflowNodePolicy.Default with { MaxVisits = 2 };
        Admit([Trigger(), Action("map", policy), ForEach("each", "map", "targets") with { Policy = policy },
                Action("publish"), Join("gather", "each") with { Policy = policy }],
            [Edge("trigger", "next", "map"), Edge("map", "next", "each"), Edge("each", "each", "publish"),
                Edge("publish", "next", "gather"), Edge("gather", "completed", "map")]);
        Succeed(Root("map"), output: Targets("one"));
        Succeed(InBranch("publish", "each", "one"));
        Succeed(Root("map", 2), output: Targets("one"));
        _second = State.Executions.Keys.Single(key => key.Node == new WorkflowNodeId("publish") && key.Branch != InBranch("publish", "each", "one").Branch);
        State = WorkflowRunFold.Replay(Run.Run, History);
    }

    void Because() => Fail(_second);

    [Fact] void should_keep_both_branch_origins() => State.Branches.Count.ShouldEqual(2);
    [Fact] void should_not_reuse_the_first_arrival() => Recorded<WorkflowBranchesJoined>().Single().Outlet.ShouldEqual(WellKnownOutlets.Partial);
    [Fact] void should_join_the_second_visit() => Recorded<WorkflowBranchesJoined>().Single().Join.Visit.ShouldEqual(2);
    [Fact] void should_keep_the_first_visit_success() => StatusOf(InBranch("publish", "each", "one")).ShouldEqual(NodeExecutionStatus.Succeeded);
}
