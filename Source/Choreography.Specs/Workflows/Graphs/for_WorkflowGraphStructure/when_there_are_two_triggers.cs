// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs.for_WorkflowGraphStructure;

public class when_there_are_two_triggers : Runs.Coordination.for_WorkflowCoordinator.given.a_coordinator
{
    List<WorkflowStructureViolation> _violations = [];

    void Because() => _violations = [.. WorkflowGraphStructure.Check(new WorkflowGraph([Trigger(), Trigger("second")], []))];

    [Fact] void should_report_only_the_extra_trigger() => _violations.Select(violation => (violation.Kind, violation.Node.Value)).ShouldContainOnly((WorkflowStructureViolationKind.MoreThanOneTrigger, "second"));
}
