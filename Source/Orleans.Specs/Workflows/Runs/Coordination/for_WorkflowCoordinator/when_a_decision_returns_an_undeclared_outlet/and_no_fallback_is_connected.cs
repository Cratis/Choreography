// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_a_decision_returns_an_undeclared_outlet;

public class and_no_fallback_is_connected : given.a_coordinator
{
    void Establish() => Admit(
        [Trigger(), Decider("route", "yes"), Action("yes-path")],
        [Edge("trigger", "next", "route"), Edge("route", "yes", "yes-path")]);

    void Because() => Succeed(Root("route"), "maybe");

    [Fact] void should_fail_the_node_unrouted() => Recorded<WorkflowNodeFailed>().Single().RoutedTo.ShouldEqual(OutletName.None);
    [Fact] void should_not_schedule_anything() => ScheduledNow.ShouldBeEmpty();
    [Fact] void should_fail_the_run() => Recorded<WorkflowRunFailed>().ShouldNotBeEmpty();
}
