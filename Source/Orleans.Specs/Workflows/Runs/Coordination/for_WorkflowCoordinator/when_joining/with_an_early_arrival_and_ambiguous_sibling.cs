// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;
using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_joining;

public class with_an_early_arrival_and_ambiguous_sibling : given.a_forked_branch
{
    void Because() => Report(InBranch("slow", "each", "one"), WorkflowNodeOutcome.Ambiguous("unknown effect"));

    [Fact] void should_take_the_partial_outlet() => Recorded<WorkflowBranchesJoined>().Single().Outlet.ShouldEqual(WellKnownOutlets.Partial);
    [Fact] void should_preserve_the_unknown_branch_outcome() => Recorded<WorkflowBranchesJoined>().Single().Results.Value.ShouldContain("\"status\":\"reconciliationRequired\"");
}
