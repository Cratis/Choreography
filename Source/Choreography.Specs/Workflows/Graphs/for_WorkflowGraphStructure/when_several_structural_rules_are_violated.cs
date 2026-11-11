// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs.for_WorkflowGraphStructure;

public class when_several_structural_rules_are_violated : Runs.Coordination.for_WorkflowCoordinator.given.a_coordinator
{
    List<WorkflowStructureViolation> _violations = [];

    void Because() => _violations = [.. WorkflowGraphStructure.Check(new WorkflowGraph(
        [Trigger(), Trigger("second"), Action("a"), Action("a"), Timer("wait", TimeSpan.FromMinutes(1)) with { Configuration = "garbage" }, ForEach("each", "trigger", "items")],
        [Edge("trigger", "undeclared", "missing"), Edge("a", "next", "a")]))];

    [Fact] void should_keep_the_existing_violation_order() => string.Join(",", _violations.Select(violation => $"{violation.Kind}:{violation.Node.Value}")).ShouldEqual("DuplicateNode:a,MoreThanOneTrigger:second,UnknownNode:missing,UndeclaredOutlet:trigger,InvalidConfiguration:wait,ForEachWithoutSingleJoin:each,UnboundedCycle:a");
}
