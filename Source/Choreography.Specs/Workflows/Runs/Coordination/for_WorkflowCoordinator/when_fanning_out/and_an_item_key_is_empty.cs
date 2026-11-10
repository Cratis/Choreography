// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_fanning_out;

public class and_an_item_key_is_empty : given.a_fan_out
{
    void Establish() => AdmitFanOut();

    void Because() => Succeed(Root("map"), output: "{\"targets\":[{\"key\":\"\"}]}");

    [Fact] void should_fail_the_for_each_as_invalid_output() => Recorded<WorkflowNodeFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.InvalidOutput);
    [Fact] void should_say_every_item_needs_a_key() => Recorded<WorkflowNodeFailed>().Single().Reason.Value.ShouldEqual($"Every item of '{Root("each-target")}' must be an object with a string 'key'");
}
