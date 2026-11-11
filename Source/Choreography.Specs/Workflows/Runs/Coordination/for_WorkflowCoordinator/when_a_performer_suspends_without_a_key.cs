// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_performer_suspends_without_a_key : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Action("ask", WorkflowNodePolicy.Default, "answered")], [Edge("trigger", "next", "ask")]);

    void Because() => Report(Root("ask"), WorkflowNodeOutcome.Suspended(string.Empty));

    [Fact] void should_not_open_a_wait() => Recorded<WorkflowWaitOpened>().ShouldBeEmpty();
    [Fact] void should_fail_the_attempt_as_invalid_output() => Recorded<WorkflowNodeAttemptFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.InvalidOutput);
}
