// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs.for_WorkflowGraphStructure;

public class when_for_eaches_are_nested : Runs.Coordination.for_WorkflowCoordinator.given.a_coordinator
{
    List<WorkflowStructureViolation> _violations = [];

    void Because() => _violations = [.. WorkflowGraphStructure.Check(new WorkflowGraph(
        [Trigger(), ForEach("outer", "trigger", "items"), ForEach("inner", "outer", "items"), Join("outer-join", "outer"), Join("inner-join", "inner")],
        [Edge("outer", "each", "inner"), Edge("inner", "each", "inner-join"), Edge("inner-join", "completed", "outer-join")]))];

    [Fact] void should_report_the_nested_for_each() => _violations.Select(violation => (violation.Kind, violation.Node.Value)).ShouldContainOnly((WorkflowStructureViolationKind.NestedForEachNotSupported, "inner"));
}
