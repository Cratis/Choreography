// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_a_decision_returns_an_undeclared_outlet;

public class and_a_fallback_is_connected : given.a_coordinator
{
    void Establish() => Admit(
        [Trigger(), Decider("route", "yes", "fallback"), Action("yes-path"), Action("safe")],
        [Edge("trigger", "next", "route"), Edge("route", "yes", "yes-path"), Edge("route", "fallback", "safe")]);

    void Because() => Succeed(Root("route"), "maybe");

    [Fact] void should_fail_the_attempt_as_an_invalid_outlet() => Recorded<WorkflowNodeAttemptFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.InvalidOutlet);
    [Fact] void should_route_the_failure_to_the_fallback() => Recorded<WorkflowNodeFailed>().Single().RoutedTo.ShouldEqual(WellKnownOutlets.Fallback);
    [Fact] void should_schedule_the_fallback() => ScheduledNow.ShouldContainOnly(Root("safe"));
}
