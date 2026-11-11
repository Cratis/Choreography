// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs.for_WorkflowGraphStructure;

public class when_a_for_each_has_no_join : Runs.Coordination.for_WorkflowCoordinator.given.a_coordinator
{
    List<WorkflowStructureViolation> _violations = [];

    void Because() => _violations = [.. WorkflowGraphStructure.Check(new WorkflowGraph([Trigger(), ForEach("each", "trigger", "items")], []))];

    [Fact] void should_report_the_for_each() => _violations.Select(violation => (violation.Kind, violation.Node.Value)).ShouldContainOnly((WorkflowStructureViolationKind.ForEachWithoutSingleJoin, "each"));
}
