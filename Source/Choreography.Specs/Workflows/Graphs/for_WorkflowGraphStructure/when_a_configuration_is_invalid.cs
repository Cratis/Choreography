// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs.for_WorkflowGraphStructure;

public class when_a_configuration_is_invalid : Runs.Coordination.for_WorkflowCoordinator.given.a_coordinator
{
    List<WorkflowStructureViolation> _violations = [];

    void Because() => _violations = [.. WorkflowGraphStructure.Check(new WorkflowGraph(
        [Trigger(), ForEach("each", "trigger", "items") with { Configuration = "garbage" }, Join("join", "each") with { Configuration = "garbage" }, Timer("wait", TimeSpan.FromMinutes(1)) with { Configuration = "garbage" }],
        []))];

    [Fact] void should_report_each_node_with_invalid_configuration() => _violations.Where(violation => violation.Kind == WorkflowStructureViolationKind.InvalidConfiguration).Select(violation => violation.Node.Value).ShouldContainOnly("each", "join", "wait");
    [Fact] void should_still_report_the_for_each_without_a_valid_join() => _violations.Where(violation => violation.Kind == WorkflowStructureViolationKind.ForEachWithoutSingleJoin).Select(violation => violation.Node.Value).ShouldContainOnly("each");
    [Fact] void should_report_only_configuration_and_join_violations() => _violations.Count.ShouldEqual(4);
}
