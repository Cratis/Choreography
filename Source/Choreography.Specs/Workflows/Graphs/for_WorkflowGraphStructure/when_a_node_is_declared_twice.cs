// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs.for_WorkflowGraphStructure;

public class when_a_node_is_declared_twice : Runs.Coordination.for_WorkflowCoordinator.given.a_coordinator
{
    List<WorkflowStructureViolation> _violations = [];

    void Because() => _violations = [.. WorkflowGraphStructure.Check(new WorkflowGraph([Trigger(), Action("a"), Action("a")], []))];

    [Fact] void should_report_the_duplicate_node() => _violations.Select(violation => (violation.Kind, violation.Node.Value)).ShouldContainOnly((WorkflowStructureViolationKind.DuplicateNode, "a"));
}
