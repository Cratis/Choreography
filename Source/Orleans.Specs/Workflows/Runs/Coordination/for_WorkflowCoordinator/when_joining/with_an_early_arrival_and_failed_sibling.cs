// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_joining;

public class with_an_early_arrival_and_failed_sibling : given.a_forked_branch
{
    void Because() => Fail(InBranch("slow", "each", "one"));

    [Fact] void should_take_the_partial_outlet() => Recorded<WorkflowBranchesJoined>().Single().Outlet.ShouldEqual(WellKnownOutlets.Partial);
    [Fact] void should_report_the_branch_failure() => Recorded<WorkflowBranchesJoined>().Single().Results.Value.ShouldContain("\"status\":\"failed\"");
}
