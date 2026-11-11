// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs.for_WorkflowGraphStructure;

public class when_an_edge_refers_to_unknown_nodes : Runs.Coordination.for_WorkflowCoordinator.given.a_coordinator
{
    List<WorkflowStructureViolation> _violations = [];

    void Because() => _violations = [.. WorkflowGraphStructure.Check(new WorkflowGraph(
        [Trigger()],
        [Edge("missing-from", "undeclared", "also-missing"), Edge("trigger", "next", "missing-to")]))];

    [Fact] void should_report_the_unknown_source_and_destination() => _violations.Select(violation => (violation.Kind, violation.Node.Value)).ShouldContainOnly((WorkflowStructureViolationKind.UnknownNode, "missing-from"), (WorkflowStructureViolationKind.UnknownNode, "missing-to"));
    [Fact] void should_skip_the_destination_of_an_unknown_source() => _violations.Select(violation => violation.Node.Value).ShouldNotContain("also-missing");
}
