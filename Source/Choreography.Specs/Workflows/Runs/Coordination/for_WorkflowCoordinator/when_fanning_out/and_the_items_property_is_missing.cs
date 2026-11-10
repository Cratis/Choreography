// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_fanning_out;

public class and_the_items_property_is_missing : given.a_fan_out
{
    void Establish() => AdmitFanOut();

    void Because() => Succeed(Root("map"), output: "{}");

    [Fact] void should_fail_the_for_each_as_invalid_output() => Recorded<WorkflowNodeFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.InvalidOutput);
    [Fact] void should_say_the_property_is_not_a_list() => Recorded<WorkflowNodeFailed>().Single().Reason.Value.ShouldEqual($"'targets' is not a list of items for '{Root("each-target")}'");
    [Fact] void should_not_fan_out() => Recorded<WorkflowBranchesFannedOut>().ShouldBeEmpty();
}
