// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_an_attempt_fails;

public class and_attempts_are_exhausted_without_one : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Action("a"), Action("b")], [Edge("trigger", "next", "a"), Edge("a", "next", "b")]);

    void Because() => Fail(Root("a"));

    [Fact] void should_fail_the_node_unrouted() => Recorded<WorkflowNodeFailed>().Single().RoutedTo.ShouldEqual(OutletName.None);
    [Fact] void should_record_what_never_ran_as_unreachable() => Recorded<WorkflowNodesUnreachable>().Single().Executions.ShouldContainOnly(Root("b"));
    [Fact] void should_fail_the_run() => Recorded<WorkflowRunFailed>().ShouldNotBeEmpty();
}
