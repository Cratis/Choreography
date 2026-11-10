// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_fanning_out;

public class and_the_output_is_not_an_object : given.a_fan_out
{
    void Establish() => AdmitFanOut();

    void Because() => Succeed(Root("map"), output: "[1,2]");

    [Fact] void should_fail_the_for_each_as_invalid_output() => Recorded<WorkflowNodeFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.InvalidOutput);
    [Fact] void should_say_the_output_is_not_a_json_object() => Recorded<WorkflowNodeFailed>().Single().Reason.Value.ShouldEqual($"The output holding the items of '{Root("each-target")}' is not a JSON object");
    [Fact] void should_record_the_failed_attempt_first() => Recorded<WorkflowNodeAttemptFailed>().Single().Attempt.ShouldEqual(WorkflowAttempt.First);
    [Fact] void should_not_fan_out() => Recorded<WorkflowBranchesFannedOut>().ShouldBeEmpty();
}
