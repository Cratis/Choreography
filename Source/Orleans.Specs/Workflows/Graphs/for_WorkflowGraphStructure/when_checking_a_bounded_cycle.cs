// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Graphs.for_WorkflowGraphStructure;

public class when_checking_a_bounded_cycle : Runs.Coordination.for_WorkflowCoordinator.given.a_coordinator
{
    List<WorkflowStructureViolation> _violations = [];

    void Because() => _violations = [.. WorkflowGraphStructure.Check(new WorkflowGraph(
        [Trigger(), Action("a", WorkflowNodePolicy.Default with { MaxVisits = 3 }), Action("b", WorkflowNodePolicy.Default with { MaxVisits = 3 })],
        [Edge("trigger", "next", "a"), Edge("a", "next", "b"), Edge("b", "next", "a")]))];

    [Fact] void should_accept_the_explicit_visit_limits() => _violations.ShouldBeEmpty();
}
