// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_output_exceeds_the_size_limit : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Action("a"), Action("b")], [Edge("trigger", "next", "a"), Edge("a", "next", "b")]);

    void Because() => Succeed(Root("a"), output: $$"""{"text":"{{new string('x', Contracts.WorkflowData.MaxBytes)}}"}""");

    [Fact] void should_fail_the_attempt_as_invalid_output() => Recorded<WorkflowNodeAttemptFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.InvalidOutput);
    [Fact] void should_not_record_the_output() => Recorded<WorkflowNodeSucceeded>().ShouldBeEmpty();
    [Fact] void should_not_continue() => ScheduledNow.ShouldBeEmpty();
}
