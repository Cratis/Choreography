// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs.for_WorkflowGraphStructure;

public class when_checking_a_cycle : Runs.Coordination.for_WorkflowCoordinator.given.a_coordinator
{
    List<WorkflowStructureViolation> _violations = [];

    void Because() => _violations = [.. WorkflowGraphStructure.Check(new WorkflowGraph(
        [Trigger(), Action("a"), Action("b")],
        [Edge("trigger", "next", "a"), Edge("a", "next", "b"), Edge("b", "next", "a")]))];

    [Fact] void should_reject_each_unbounded_node_in_the_cycle() => _violations.Where(violation => violation.Kind == WorkflowStructureViolationKind.UnboundedCycle).Select(violation => violation.Node.Value).ShouldContainOnly("a", "b");
    [Fact] void should_not_reject_the_trigger_outside_the_cycle() => _violations.Select(violation => violation.Node.Value).ShouldNotContain("trigger");
}
